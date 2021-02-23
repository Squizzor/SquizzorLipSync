using AltV.Net;
using AltV.Net.Async;
using AltV.Net.Data;
using AltV.Net.Elements.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using TeamSpeak3QueryApi.Net;

namespace TSLipSync
{
    public class TSLipSyncResource : AsyncResource
    {
        public class PlayerComparer : IEqualityComparer<IPlayer>
        {
            public bool Equals(IPlayer player1, IPlayer player2)
            {
                return player1.Id == player2.Id;
            }

            public int GetHashCode([DisallowNull] IPlayer player)
            {
                return player.Id;
            }
        }

        private QueryClient _teamspeakQueryClient;
        private Dictionary<IPlayer, HashSet<IPlayer>> _talkingPlayersWithNearByPlayers = new Dictionary<IPlayer, HashSet<IPlayer>>(new PlayerComparer());

        private string TeamspeakQueryAddress { get; set; } = Constants.Settings.Defaults.TeamspeakQueryAddress;
        private short TeamspeakQueryPort { get; set; } = Constants.Settings.Defaults.TeamspeakQueryPort;
        private short TeamspeakPort { get; set; } = Constants.Settings.Defaults.TeamspeakPort;
        private string TeamspeakUsername { get; set; } = Constants.Settings.Defaults.TeamspeakUsername;
        private string TeamspeakPassword { get; set; } = Constants.Settings.Defaults.TeamspeakPassword;
        private string TeamspeakChannel { get; set; } = Constants.Settings.Defaults.TeamspeakChannel;
        private string TeamspeakClientPropertyToCheck { get; set; } = Constants.Settings.Defaults.TeamspeakClientPropertyToCheck;
        private int CheckIntervalInMs { get; set; } = Constants.Settings.Defaults.CheckIntervalInMs;
        private int SynchronisationRangeInM { get; set; } = Constants.Settings.Defaults.SynchronisationRangeInM;

        private void ReadConfigurationFile()
        {
            var filename = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Constants.Files.ConfigurationFile);

            var configurationFile = new XmlDocument();
            configurationFile.Load(filename);

            foreach (XmlElement setting in configurationFile.DocumentElement.ChildNodes)
            {
                switch (setting.Name)
                {
                    case Constants.Settings.TeamspeakQueryAddress:
                        TeamspeakQueryAddress = setting.InnerText;
                        break;
                    case Constants.Settings.TeamspeakQueryPort:
                        TeamspeakQueryPort = Convert.ToInt16(setting.InnerText);
                        break;
                    case Constants.Settings.TeamspeakPort:
                        TeamspeakPort = Convert.ToInt16(setting.InnerText);
                        break;
                    case Constants.Settings.TeamspeakUsername:
                        TeamspeakUsername = setting.InnerText;
                        break;
                    case Constants.Settings.TeamspeakPassword:
                        TeamspeakPassword = setting.InnerText;
                        break;
                    case Constants.Settings.TeamspeakChannel:
                        TeamspeakChannel = setting.InnerText;
                        break;
                    case Constants.Settings.TeamspeakClientPropertyToCheck:
                        TeamspeakClientPropertyToCheck = setting.InnerText;
                        break;
                    case Constants.Settings.CheckIntervalInMs:
                        CheckIntervalInMs = Convert.ToInt32(setting.InnerText);
                        break;
                    case Constants.Settings.SynchronisationRangeInM:
                        SynchronisationRangeInM = Convert.ToInt32(setting.InnerText);
                        break;
                }
            }
        }

        public override void OnStart()
        {
            try
            {
                AltAsync.OnClient<IPlayer, string>(Constants.ClientEvents.OnIdentifierTransmission, OnIdentifierTransmission);

                ReadConfigurationFile();

                _teamspeakQueryClient = new QueryClient(TeamspeakQueryAddress, TeamspeakQueryPort);

                Task.Run(() =>
                {
                    while (true)
                    {
                        try
                        {
                            _talkingPlayersWithNearByPlayers.Clear();
                            CheckSpeakingClients().Wait();
                        }
                        catch (Exception e)
                        {
                            Alt.Log(e.Message);
                        }
                    }
                });
            }
            catch (Exception e)
            {
                Alt.Log(e.Message);
            }
        }

        public override void OnStop()
        {
            _teamspeakQueryClient.Disconnect();
        }

        public void OnIdentifierTransmission(IPlayer player, string identifier)
        {
            player.SetSyncedMetaData(Constants.PlayerProperties.TeamspeakIdentifier, identifier);
        }

        private double GetDistance(Position position1, Position position2) 
        {
            return Math.Sqrt(Math.Pow(position1.X - position2.X, 2) + Math.Pow(position1.Y - position2.Y, 2) + Math.Pow(position1.Z - position2.Z, 2));
        }

        private async Task CheckSpeakingClients()
        {
            Alt.Log("Connecting to Teamspeak...");

            await _teamspeakQueryClient.Connect();
            await _teamspeakQueryClient.Send("login", new Parameter("client_login_name", TeamspeakUsername), new Parameter("client_login_password", TeamspeakPassword));
            await _teamspeakQueryClient.Send("use", new Parameter("port", TeamspeakPort));

            String channelId = "";

            if (_teamspeakQueryClient.IsConnected)
            {
                Alt.Log("Successfully connected to Teamspeak.");

                var channel = await _teamspeakQueryClient.Send("channelfind", new Parameter("pattern", TeamspeakChannel));
                channelId = channel.First()["cid"].ToString();
            }

            while (_teamspeakQueryClient.IsConnected)
            {
                try
                {
                    List<String> talkingClients = new List<String>();
                    var clientList = await _teamspeakQueryClient.Send("clientlist", new Parameter("-voice", ""));

                    foreach (var client in clientList)
                    {
                        if (client["client_flag_talking"].ToString() == "1" && client["cid"].ToString() == channelId)
                        {
                            var clientProperty = client[TeamspeakClientPropertyToCheck].ToString().ToLowerInvariant();
                            talkingClients.Add(clientProperty);
                        }
                    }

                    await Alt.ForEachPlayers(new AsyncFunctionCallback<IPlayer>(async (player) =>
                    {
                        player.GetSyncedMetaData(Constants.PlayerProperties.TeamspeakIdentifier, out string identifier);

                        if (identifier != null)
                        {
                            identifier = identifier.ToLowerInvariant();

                            // Is player talking?
                            if (talkingClients.Contains(identifier))
                            {
                                // Was player not talking before?
                                if (!_talkingPlayersWithNearByPlayers.ContainsKey(player))
                                {
                                    // Inform player
                                    _ = player.EmitAsync(Constants.ClientEvents.OnStartTalking, player.Id);
                                }

                                var nearByPlayers = new HashSet<IPlayer>(new PlayerComparer());

                                // Check for near by players
                                await Alt.ForEachPlayers(new AsyncFunctionCallback<IPlayer>(async (otherPlayer) =>
                                {
                                    if (player.Id != otherPlayer.Id && GetDistance(player.Position, otherPlayer.Position) <= SynchronisationRangeInM)
                                    {
                                        nearByPlayers.Add(otherPlayer);
                                    }

                                    await Task.CompletedTask;
                                }));

                                // Was player not talking before?
                                if (!_talkingPlayersWithNearByPlayers.ContainsKey(player))
                                {
                                    // Inform all near by players
                                    foreach (IPlayer nearByPlayer in nearByPlayers)
                                    {
                                        _ = nearByPlayer.EmitAsync(Constants.ClientEvents.OnStartTalking, player.Id);
                                    }

                                    _talkingPlayersWithNearByPlayers.Add(player, nearByPlayers);
                                }
                                // Player was talking before
                                else
                                {
                                    // Inform all new joined near by players
                                    foreach (IPlayer nearByPlayer in nearByPlayers)
                                    {
                                        // Was near by player not already informed? (player joined the conversation)
                                        if (!_talkingPlayersWithNearByPlayers[player].Contains(nearByPlayer))
                                        {
                                            _ = nearByPlayer.EmitAsync(Constants.ClientEvents.OnStartTalking, player.Id);
                                            _talkingPlayersWithNearByPlayers[player].Add(nearByPlayer);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Was player talking before?
                                if (_talkingPlayersWithNearByPlayers.ContainsKey(player))
                                {
                                    // Inform player
                                    await player.EmitAsync(Constants.ClientEvents.OnStopTalking, player.Id);

                                    // Inform registered near by players
                                    foreach (IPlayer nearByPlayer in _talkingPlayersWithNearByPlayers[player])
                                    {
                                        _ = nearByPlayer.EmitAsync(Constants.ClientEvents.OnStopTalking, player.Id);
                                    }

                                    _talkingPlayersWithNearByPlayers.Remove(player);
                                }
                            }
                        }

                        await Task.CompletedTask;
                    }));

                    await Task.Delay(CheckIntervalInMs);
                }
                catch (Exception e)
                {
                    Alt.Log(e.Message);
                }
            }
        }
    }
}
