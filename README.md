# GTA5 - alt:V - Teamspeak LipSync

## IMPORTANT
If you checkout and compile the source, please make sure to also update the alt:V assembly referenes with nuget to make the resource work with the current alt:V version.

## About
This alt:V resource connects to a given Teamspeak server and queries in a specified interval which players are talking right now. It then gives this information further to each talking client and all near by players.

When the client gets those information it starts and stops the lips animation on the corresponding player.

**This resource only does the lips animation. For overall voice transmittance you have to use a Teamspeak plugin in addition (e.g. GTA5Voice.net).**

## How it works
The resource uses the Teamspeak Query interface to retrieve all clients on the specified server. It then puts all clients who are talking and in the right channel into a list. 

Then a loop will go through every player on the **alt:V server** and checks if the player of the current iteration is in the list created before. If so, that means the player is talking right now and will be informed by the server through an emitted client event. 
The server also checks for all near by player and inform them as well about the talking player.
The triggered client event will then just start the lips animation on the corresponding player model (pedal).

If the player of the current iteration couldn't be found in the talking clients list of Teamspeak, it will be checked if the player was talking before. If so, it means the player stopped talking and will be informed in the same way he was before when he started talking. Also the near by player will be informed that the player stopped talking.

This process will be repeated in a specified interval.

## How does client identification work on both sides?
To make the identification of alt:V player to Teamspeak client work, there has to be one information that is unique and is used on both sides. In most cases the character name would work the best.
For example, if you would use the character name John Doe, you would then have to set one of your Teamspeak client properties (Nickname / Description) to "John Doe" and set this identifier on the corresponding player meta data in **one** of the following ways:

##### C# Server
playerObject.SetSyncedMetaData("TsLipSyncIdentifier", "John Doe");

##### Javascript Server
playerObject.setSyncedMeta("TsLipSyncIdentifier", "John Doe");

##### Javascript Client
alt.emitServer('tslipsync:identifierTransmission', 'John Doe');

Now when you have the name "John Doe" in your players meta data (TsLipSyncIdentifier) and either your nickname or description in Teamspeak (depending on the configuration of TeamspeakClientPropertyToCheck) is also "John Doe", the resource is able to match up the ingame player and the corresponding Teamspeak client.

## How to use

### Basic Setup
Create a folder with a name of your choice (e.g. TsLipSync) in your servers resources folder. Then copy all the compiled output into it and add the folder name to the resources section in your server.cfg.

Since this is a C# resource, you'll have to make sure that you have all the necessary modules in your server root and modules folder (you can get them on the alt:V download page, when you check the "C# module" box).

### Server side
#### Setting the identification key of the player ####
As in **How does client identification work on both sides?** mentioned, you need to set the identification key for each player to make it possible to match up alt:V player with Teamspeak client. For this you have to create the "TsLipSyncIdentifier" property in the synced meta data of the player. See above examples.

#### Configuration
There is a configuration.xml which allows you to configure this resource. Most importantly are the Teamspeak server information. The resource needs a valid Teamspeak server query login to be able to work. See Teamspeak documentation on how to generate a query login. 

The Teamspeak server settings are self explanatory, other settings are:

##### TeamspeakChannel
The Teamspeak channel that will be checked for talking clients.

##### TeamspeakClientPropertyToCheck
This is the Teamspeak client property that will be compared to the specified players identification key ("TsLipSyncIdentifier"). Possible values are for example "client_nickname" or "client_description". See Teamspeak documentation for more possible values.

##### CheckIntervalInMs
This is the interval in milliseconds in which the resource will query the Teamspeak server for talking clients and inform the players about the talking states.
A lower value will decrease the latency between speaking and the lip animation, but will also increase the traffic on alt:V and Teamspeak server.

##### SynchronisationRangeInM
This will define the radius in which the near by players of a talking client will be informed about the talking states. The higher the value, the earlier you can see the talking animation of someone in the distance, but the higher will be the server traffic.

### Client side
#### Setting the identification key of the player ####
WARNING: This is not necessary, if you already set the identification key on the server side. Setting the identification key multiple times and maybe even differently can cause weird bugs.

To set the identification key on client side just trigger the following server event and pass your identification key to it:

alt.emitServer('tslipsync:identifierTransmission', 'John Doe');