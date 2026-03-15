# SLWardrobe
![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/ChochoZagorski/SLWardrobe/total) ![GitHub Release](https://img.shields.io/github/v/release/ChochoZagorski/SLWardrobe)

SLWardrobe is a plugin made with the purpose of giving players to wear schematics made with ProjectMER to enhance Roleplay and add a new way to experience the game SCP: Secret Laboratory.
It offers the ability for anyone to create "Suits" that could be worn by players.

### Dependencies
 - SCP: Secret Laboratory 14.1.1
 - Exiled 9.6.1
 - ProjectMER (and its Dependencies)

### Installation
The installation is very simple. All you need to do is go to ``%AppData%\EXILED\Plugins`` (``~/.config/EXILED/Plugins`` on Linux) and drop the ``SLWardrobe.dll``.

### Commands
Currently all commands require access to the Remote Admin Panel
 - ``slw`` Lists all available Commands in the RA panel.
 - ``slw suit (player id) (suitname)`` applies the suit that is defined in the config.
 - ``slw remove (player id)`` removes the suit the player is currently wearing.
 - ``slw checksuit (player id)`` checks if a player is wearing a suit and what suit they are wearing.
 - ``slw list [suits|weapons]`` will list the correctly configured suits and weapons. Shows what model the suit|weapon is linked to, the suits|weapons schematic amount, and if it makes the wearer invisible.
 - ``slw create [suit|weapon] [name] [type]`` creates a config file for a new suit|weapon depending on what name and assigned type/model it was assigned. EX: ``slw create suit Jeff Human``. If the command is run while prompts are empty it will list available types.
 - ``slw merge [Suit 1|Weapon 1] [Suit 2|Weapon 2] [name of output]`` Can merge the config of two suits or two weapons together into a new conifg. Does not delete the previous configs.
 - ``slw reload`` rechecks the config for changes or new suits.
 - ``slw debug`` provides debug information to the RA console such as what's active, what suits are loaded, what weapons are loaded,and what weapons are active.

### Config
The config file is auto-generated and is located at: ``%AppData%\EXILED\Configs\Plugins\s_l_wardrobe\(ServerPortHere).yml`` (``~/.config/EXILED/Configs/Plugins/s_l_wardrobe/(ServerPortHere).yml`` on Linux)

The Suit and Weapon Configs are generated and located at: ``%AppData%\Exiled\Configs\SLWardrobe\Suits|Weapons`` or on linux: ``~/.config/Exiled/Configs/SLWardrobe/Suits|Weapons``

For more information please check the wiki: https://github.com/ChochoZagorski/SLWardrobe/wiki/Configs

### Permissions
 - ``slwardrobe.suits``
