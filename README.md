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
 - ``suit (player id) (suitname)`` is to wear a suit defined in the config
 - ``checksuit (player id)`` is to check if a player is wearing a suit and what suit they are wearing
 - ``listsuits <detailed>`` is used to check what suits are configured in the config. Typing "detailed" after listsuits will list each suit in the config, how many parts does it contain and where those parts are connected to (Which Schematic to Which Bone)
 - ``removesuit (player id)``

### Config
The config file is auto-generated and is located at: ``%AppData%\EXILED\Configs\Plugins\s_l_wardrobe\(ServerPortHere).yml`` (``~/.config/EXILED/Configs/Plugins/s_l_wardrobe/(ServerPortHere).yml`` on Linux)
| config                              | type       | default       | description                                                                  |
|-------------------------------------|-----------:|:-------------:|:----------------------------------------------------------------------------:|
| is_enabled                          | bool       | true          | If the plugin is enabled                                                     |
| debug                               | bool       | false         | If debug logging to the server console is enabled                            |
| suit_update_interval                | float      | 0.33          | How often to update suit positions in seconds                                |
|                                                                                                                                                 |
| suits                               | dictionary |               | Define custom suits. Each suit has its own name and list of parts            |
| example_suit                        |            |               | The name of the suit. This is what you will type in the command "suit"       |
| description                         | string     | "Custom Suit" | The description of the suit. This is what you will see when using "listsuits"|
| parts                               | list       |               | List of parts that make up the suit                                          |
| schematic_name                      | string     | ""            | The name of the schematic that will be binded to the bone                    |
| bone_name                           | string     | ""            | The name of the bone to attach the schematic to                              |
| position_x                          | float      | 0             | Local position offset X between bone and schematic                           |
| position_y                          | float      | 0             | Local position offset Y between bone and schematic                           |
| position_z                          | float      | 0             | Local position offset Z between bone and schematic                           |
| rotation_x                          | float      | 0             | Local rotation offset X between bone and schematic                           |
| rotation_y                          | float      | 0             | Local rotation offset Y between bone and schematic                           |
| rotation_z                          | float      | 0             | Local rotation offset Z between bone and schematic                           |
| scale_x                             | float      | 1             | Schematic scale, with this you can make the schematic larger or smaller on the X |
| scale_y                             | float      | 1             | Schematic scale, with this you can make the schematic larger or smaller on the Y |
| scale_z                             | float      | 1             | Schematic scale, with this you can make the schematic larger or smaller on the Z |
| hide_for_wearer                     | bool       | false         | Hide the Bone-Schematic from the player wearing the suit (Other players will still see it) |

### Permissions
 - ``slwardrobe.suits``