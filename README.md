# SLWardrobe
SLWardrobe is a plugin made with the purpose of giving players to wear schematics made with ProjectMER to enhance Roleplay and add a new way to experience the game SCP: Secret Laboratory.
It offers the ability for anyone to create "Suits" that could be worn by players.

# Dependencies
 - SCP: Secret Laboratory 14.1.1
 - Exiled 9.6.1
 - ProjectMER (and its Dependencies)

# Installation
The installation is very simple. All you need to do is go to ``%AppData%\EXILED\Plugins`` (``~/.config/EXILED/Plugins`` on Linux) and drop the ``SLWardrobe.dll``.

# Config
The config file is auto-generated and is located at: ``%AppData%\EXILED\Configs\Plugins\s_l_wardrobe\(ServerPortHere).yml`` (``~/.config/EXILED/Configs/Plugins/s_l_wardrobe/(ServerPortHere).yml`` on Linux)

# Commands
Currently all commands require access to the Remote Admin Panel
 - ``suit (player id) (suitname)`` is to wear a suit defined in the config
 - ``checksuit (player id)`` is to check if a player is wearing a suit and what suit they are wearing
 - ``listsuits <detailed>`` is used to check what suits are configured in the config. Typing "detailed" after listsuits will list each suit in the config, how many parts does it contain and where those parts are connected to (Which Schematic to Which Bone)
 - ``removesuit (player id)``

# Permissions
 - ``slwardrobe.suits``