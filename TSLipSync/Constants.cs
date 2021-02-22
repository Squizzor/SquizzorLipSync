using System;
using System.Collections.Generic;
using System.Text;

namespace TSLipSync
{
    class Constants
    {
        public static class ClientEvents
        {
            public const string OnIdentifierTransmission = "tslipsync:identifierTransmission";
            public const string OnStartTalking = "tslipsync:onStartTalking";
            public const string OnStopTalking = "tslipsync:onStopTalking";
        }

        public static class PlayerProperties
        {
            public const string TeamspeakIdentifier = "TsLipSyncIdentifier";
        }

        public static class Files
        {
            public const string ConfigurationFile = "configuration.xml";
        }

        public static class Settings
        {
            public const string TeamspeakQueryAddress = "TeamspeakQueryAddress";
            public const string TeamspeakQueryPort = "TeamspeakQueryPort";
            public const string TeamspeakPort = "TeamspeakPort";
            public const string TeamspeakUsername = "TeamspeakUsername";
            public const string TeamspeakPassword = "TeamspeakPassword";
            public const string TeamspeakChannel = "TeamspeakChannel";
            public const string TeamspeakClientPropertyToCheck = "TeamspeakClientPropertyToCheck";
            public const string DecodeClientPropertyWithBase64 = "DecodeClientPropertyWithBase64";
            public const string StripSpacesInClientProperty = "StripSpacesInClientProperty";  
            public const string CheckIntervalInMs = "CheckIntervalInMs";
            public const string SynchronisationRangeInM = "SynchronisationRangeInM";

            public class Defaults
            {
                public const string TeamspeakQueryAddress = "127.0.0.1";
                public const short TeamspeakQueryPort = 10011;
                public const short TeamspeakPort = 9987;
                public const string TeamspeakUsername = "admin";
                public const string TeamspeakPassword = "";
                public const string TeamspeakChannel = "Ingame";
                public const string TeamspeakClientPropertyToCheck = "client_nickname";
                public const bool DecodeClientPropertyWithBase64 = false;
                public const bool StripSpacesInClientProperty = false;
                public const int CheckIntervalInMs = 500;
                public const int SynchronisationRangeInM = 10;
            }
        }
    }
}
