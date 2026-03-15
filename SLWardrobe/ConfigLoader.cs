using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using SLWardrobe.Models;

#if EXILED
using Exiled.API.Features;
#else
using Log = LabApi.Features.Console.Logger;
#endif

namespace SLWardrobe
{
    public static class ConfigLoader
    {
        private static readonly ISerializer Serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        private static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        public static string PluginFolder { get; private set; }

        public static string SuitsFolder => Path.Combine(PluginFolder, "Suits");
        public static string WeaponsFolder => Path.Combine(PluginFolder, "Weapons");

        private static readonly Dictionary<string, SuitDefinition> LoadedSuits = new Dictionary<string, SuitDefinition>();
        private static readonly Dictionary<string, WeaponDefinition> LoadedWeapons = new Dictionary<string, WeaponDefinition>();

        public static IReadOnlyDictionary<string, SuitDefinition> Suits => LoadedSuits;
        public static IReadOnlyDictionary<string, WeaponDefinition> Weapons => LoadedWeapons;

        public static void SetPluginFolder(string path)
        {
            PluginFolder = path;
            Log.Debug($"[ConfigLoader] Plugin folder set to: {PluginFolder}");
        }

        public static void EnsureDirectories()
        {
            if (string.IsNullOrEmpty(PluginFolder))
            {
                Log.Error("[ConfigLoader] PluginFolder not set. Call SetPluginFolder() first.");
                return;
            }

            try
            {
                Directory.CreateDirectory(SuitsFolder);
                Directory.CreateDirectory(WeaponsFolder);
                Log.Debug($"[ConfigLoader] Directories verified at: {PluginFolder}");
            }
            catch (Exception ex)
            {
                Log.Error($"[ConfigLoader] Failed to create directories: {ex}");
                Log.Error($"[ConfigLoader] PluginFolder = '{PluginFolder}'");
            }
        }

        public static void LoadAll()
        {
            EnsureDirectories();
            MigrateOldConfig();
            LoadSuits();
            LoadWeapons();
        }

        public static void ReloadAll()
        {
            LoadedSuits.Clear();
            LoadedWeapons.Clear();
            LoadAll();
        }

        #region Loading

        private static void LoadSuits()
        {
            LoadedSuits.Clear();

            if (!Directory.Exists(SuitsFolder))
            {
                Log.Warn($"[ConfigLoader] Suits folder missing: {SuitsFolder}");
                return;
            }

            foreach (var file in Directory.GetFiles(SuitsFolder, "*.yml"))
            {
                try
                {
                    var yaml = File.ReadAllText(file);
                    var suit = Deserializer.Deserialize<SuitDefinition>(yaml);

                    if (suit == null)
                    {
                        Log.Warn($"[ConfigLoader] Failed to parse suit from {Path.GetFileName(file)}");
                        continue;
                    }

                    var key = string.IsNullOrEmpty(suit.Name) || suit.Name == "unnamed_suit"
                        ? Path.GetFileNameWithoutExtension(file)
                        : suit.Name;

                    suit.Name = key;
                    LoadedSuits[key] = suit;
                    Log.Debug($"[ConfigLoader] Loaded suit: {key} ({suit.Parts.Count} parts)");
                }
                catch (Exception ex)
                {
                    Log.Error($"[ConfigLoader] Error loading suit '{Path.GetFileName(file)}': {ex.Message}");
                }
            }

            Log.Info($"[ConfigLoader] Loaded {LoadedSuits.Count} suit(s).");
        }

        private static void LoadWeapons()
        {
            LoadedWeapons.Clear();

            if (!Directory.Exists(WeaponsFolder))
            {
                Log.Warn($"[ConfigLoader] Weapons folder missing: {WeaponsFolder}");
                return;
            }

            foreach (var file in Directory.GetFiles(WeaponsFolder, "*.yml"))
            {
                try
                {
                    var yaml = File.ReadAllText(file);
                    var weapon = Deserializer.Deserialize<WeaponDefinition>(yaml);

                    if (weapon == null)
                    {
                        Log.Warn($"[ConfigLoader] Failed to parse weapon from {Path.GetFileName(file)}");
                        continue;
                    }

                    var key = string.IsNullOrEmpty(weapon.Name) || weapon.Name == "unnamed_weapon"
                        ? Path.GetFileNameWithoutExtension(file)
                        : weapon.Name;

                    weapon.Name = key;
                    LoadedWeapons[key] = weapon;
                    Log.Debug($"[ConfigLoader] Loaded weapon: {key} ({weapon.Parts.Count} parts)");
                }
                catch (Exception ex)
                {
                    Log.Error($"[ConfigLoader] Error loading weapon '{Path.GetFileName(file)}': {ex.Message}");
                }
            }

            Log.Info($"[ConfigLoader] Loaded {LoadedWeapons.Count} weapon(s).");
        }

        #endregion

        #region Legacy Config Migration

        /// Scans for old monolithic configs (files with root-level "suits:" key),
        /// splits each suit into its own file under Suits/, then renames the old file to .migrated.
        private static void MigrateOldConfig()
        {
            var candidates = new List<string>();

            if (Directory.Exists(PluginFolder))
            {
                foreach (var file in Directory.GetFiles(PluginFolder, "*.yml"))
                    candidates.Add(file);
            }
            
            try
            {
                var parentDir = Path.GetDirectoryName(PluginFolder);
                if (!string.IsNullOrEmpty(parentDir) && Directory.Exists(parentDir))
                {
                    foreach (var file in Directory.GetFiles(parentDir, "*.yml"))
                    {
                        var name = Path.GetFileName(file).ToLower();
                        if (name.Contains("slwardrobe") || name.Contains("sl_wardrobe"))
                            candidates.Add(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"[ConfigLoader] Could not scan parent dir for legacy files: {ex.Message}");
            }

            foreach (var file in candidates)
            {
                try
                {
                    if (!IsOldFormatConfig(file))
                        continue;

                    Log.Warn($"[ConfigLoader] Found legacy config: {file}");
                    MigrateLegacyFile(file);
                }
                catch (Exception ex)
                {
                    Log.Error($"[ConfigLoader] Failed to migrate '{Path.GetFileName(file)}': {ex.Message}");
                }
            }
        }

        private static bool IsOldFormatConfig(string filePath)
        {
            try
            {
                var content = File.ReadAllText(filePath);

                if (!content.Contains("suits:"))
                    return false;

                var data = Deserializer.Deserialize<Dictionary<string, object>>(content);
                if (data == null)
                    return false;

                return data.ContainsKey("suits") &&
                       (data.ContainsKey("is_enabled") || data.ContainsKey("suit_update_interval"));
            }
            catch
            {
                return false;
            }
        }

        private static void MigrateLegacyFile(string filePath)
        {
            var yaml = File.ReadAllText(filePath);
            var oldConfig = Deserializer.Deserialize<LegacyConfig>(yaml);

            if (oldConfig?.Suits == null || oldConfig.Suits.Count == 0)
            {
                Log.Warn("[ConfigLoader] Legacy config had no suits to migrate.");
                return;
            }

            int migrated = 0;

            foreach (var kvp in oldConfig.Suits)
            {
                var suitName = kvp.Key;
                var suit = kvp.Value;

                if (suit == null) continue;

                suit.Name = suitName;
                var targetPath = Path.Combine(SuitsFolder, $"{suitName}.yml");

                if (File.Exists(targetPath))
                {
                    Log.Warn($"[ConfigLoader] Skipping migration of '{suitName}' — file already exists.");
                    continue;
                }

                try
                {
                    var suitYaml = Serializer.Serialize(suit);
                    var header = $"# Migrated from legacy config: {Path.GetFileName(filePath)}\n" +
                                 $"# Migrated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                                 $"# Wearer type: {suit.WearerType} | Parts: {suit.Parts.Count}\n\n";

                    File.WriteAllText(targetPath, header + suitYaml);
                    migrated++;
                    Log.Info($"[ConfigLoader] Migrated suit '{suitName}' ({suit.Parts.Count} parts)");
                }
                catch (Exception ex)
                {
                    Log.Error($"[ConfigLoader] Failed to write migrated suit '{suitName}': {ex.Message}");
                }
            }

            if (migrated > 0)
            {
                var backupPath = filePath + ".migrated";
                try
                {
                    if (File.Exists(backupPath))
                        backupPath = filePath + $".migrated_{DateTime.Now:yyyyMMdd_HHmmss}";

                    File.Move(filePath, backupPath);
                    Log.Warn($"[ConfigLoader] Migrated {migrated} suit(s). Old config renamed to: {Path.GetFileName(backupPath)}");
                }
                catch (Exception ex)
                {
                    Log.Warn($"[ConfigLoader] Could not rename old config (suits were still migrated): {ex.Message}");
                }
            }
        }

        #endregion

        #region Template Creation

        private static string BuildTimestamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static bool CreateSuitTemplate(string name, string wearerType = "Human", string creatorInfo = "Unknown")
        {
            EnsureDirectories();

            var filePath = Path.Combine(SuitsFolder, $"{name}.yml");
            if (File.Exists(filePath))
            {
                Log.Warn($"[ConfigLoader] Suit '{name}' already exists.");
                return false;
            }

            var template = new SuitDefinition
            {
                Name = name,
                Description = $"Custom suit: {name}",
                MakeWearerInvisible = false,
                WearerType = wearerType,
                Parts = new List<SuitPartDefinition>
                {
                    new SuitPartDefinition
                    {
                        SchematicName = "your_schematic_here",
                        BoneName = "body"
                    }
                }
            };

            try
            {
                var yaml = Serializer.Serialize(template);
                var header = $"# SLWardrobe Suit Definition\n" +
                             $"# File: {name}.yml\n" +
                             $"# Created by: {creatorInfo}\n" +
                             $"# Created at: {BuildTimestamp()}\n" +
                             $"# Available bones for {wearerType}: {string.Join(", ", BoneMappings.GetAvailableBones(wearerType))}\n" +
                             $"# Available wearer types: {string.Join(", ", BoneMappings.GetWearerTypes())}\n\n";

                File.WriteAllText(filePath, header + yaml);
                Log.Info($"[ConfigLoader] Created suit template: {filePath} (by {creatorInfo})");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[ConfigLoader] Failed to create suit template: {ex.Message}");
                return false;
            }
        }

        public static bool CreateWeaponTemplate(string name, string itemType = "Flashlight", string creatorInfo = "Unknown")
        {
            EnsureDirectories();

            var filePath = Path.Combine(WeaponsFolder, $"{name}.yml");
            if (File.Exists(filePath))
            {
                Log.Warn($"[ConfigLoader] Weapon '{name}' already exists.");
                return false;
            }

            var template = new WeaponDefinition
            {
                Name = name,
                Description = $"Custom weapon: {name}",
                Detection = new ItemDetection
                {
                    Type = "VanillaItem",
                    Identifier = itemType,
                    CustomItemSource = ""
                },
                AttachBone = "rightforearm",
                WearerType = "Human",
                Parts = new List<WeaponPartDefinition>
                {
                    new WeaponPartDefinition
                    {
                        SchematicName = "your_weapon_schematic_here"
                    }
                }
            };

            try
            {
                var yaml = Serializer.Serialize(template);
                var header = $"# SLWardrobe Weapon Definition\n" +
                             $"# File: {name}.yml\n" +
                             $"# Created by: {creatorInfo}\n" +
                             $"# Created at: {BuildTimestamp()}\n" +
                             $"# Detection types: VanillaItem, CustomItem\n" +
                             $"# For VanillaItem, use ItemType names (Flashlight, GunCOM15, Medkit, etc.)\n" +
                             $"# For CustomItem, use the custom item's ID or name\n\n";

                File.WriteAllText(filePath, header + yaml);
                Log.Info($"[ConfigLoader] Created weapon template: {filePath} (by {creatorInfo})");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[ConfigLoader] Failed to create weapon template: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Merge Operations

        public static bool MergeSuits(SuitDefinition suit1, SuitDefinition suit2, string outputName,
            string source1Name, string source2Name, string mergerInfo = "Unknown")
        {
            EnsureDirectories();

            var filePath = Path.Combine(SuitsFolder, $"{outputName}.yml");
            if (File.Exists(filePath))
            {
                Log.Warn($"[ConfigLoader] Suit '{outputName}' already exists.");
                return false;
            }

            var merged = new SuitDefinition
            {
                Name = outputName,
                Description = $"Merged from: {source1Name} + {source2Name}",
                MakeWearerInvisible = suit1.MakeWearerInvisible || suit2.MakeWearerInvisible,
                WearerType = suit1.WearerType,
                Parts = new List<SuitPartDefinition>()
            };

            merged.Parts.AddRange(suit1.Parts);
            merged.Parts.AddRange(suit2.Parts);

            var source1Bones = suit1.Parts.Select(p => p.BoneName).Distinct().ToList();
            var source2Bones = suit2.Parts.Select(p => p.BoneName).Distinct().ToList();
            var overlapping = source1Bones.Intersect(source2Bones).ToList();

            try
            {
                var yaml = Serializer.Serialize(merged);
                var header = $"# SLWardrobe Suit Definition (Merged)\n" +
                             $"# File: {outputName}.yml\n" +
                             $"# Merged by: {mergerInfo}\n" +
                             $"# Merged at: {BuildTimestamp()}\n" +
                             $"# Source 1: {source1Name} ({suit1.Parts.Count} parts, bones: {string.Join(", ", source1Bones)})\n" +
                             $"# Source 2: {source2Name} ({suit2.Parts.Count} parts, bones: {string.Join(", ", source2Bones)})\n";

                if (overlapping.Count > 0)
                {
                    header += $"# WARNING: Overlapping bones: {string.Join(", ", overlapping)}\n" +
                              $"# You may want to review and remove duplicates manually.\n";
                }

                header += $"# Available bones for {merged.WearerType}: {string.Join(", ", BoneMappings.GetAvailableBones(merged.WearerType))}\n\n";

                File.WriteAllText(filePath, header + yaml);
                Log.Info($"[ConfigLoader] Merged suits '{source1Name}' + '{source2Name}' into '{outputName}' (by {mergerInfo})");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[ConfigLoader] Failed to merge suits: {ex.Message}");
                return false;
            }
        }

        public static bool MergeWeapons(WeaponDefinition weapon1, WeaponDefinition weapon2, string outputName,
            string source1Name, string source2Name, string mergerInfo = "Unknown")
        {
            EnsureDirectories();

            var filePath = Path.Combine(WeaponsFolder, $"{outputName}.yml");
            if (File.Exists(filePath))
            {
                Log.Warn($"[ConfigLoader] Weapon '{outputName}' already exists.");
                return false;
            }

            var merged = new WeaponDefinition
            {
                Name = outputName,
                Description = $"Merged from: {source1Name} + {source2Name}",
                Detection = weapon1.Detection,
                AttachBone = weapon1.AttachBone,
                WearerType = weapon1.WearerType,
                Parts = new List<WeaponPartDefinition>()
            };

            merged.Parts.AddRange(weapon1.Parts);
            merged.Parts.AddRange(weapon2.Parts);

            bool detectionDiffers = weapon1.Detection.Type != weapon2.Detection.Type ||
                                    weapon1.Detection.Identifier != weapon2.Detection.Identifier;

            try
            {
                var yaml = Serializer.Serialize(merged);
                var header = $"# SLWardrobe Weapon Definition (Merged)\n" +
                             $"# File: {outputName}.yml\n" +
                             $"# Merged by: {mergerInfo}\n" +
                             $"# Merged at: {BuildTimestamp()}\n" +
                             $"# Source 1: {source1Name} ({weapon1.Parts.Count} parts)\n" +
                             $"# Source 2: {source2Name} ({weapon2.Parts.Count} parts)\n";

                if (detectionDiffers)
                {
                    header += $"# NOTE: Sources had different detection configs.\n" +
                              $"#   Using: '{source1Name}': {weapon1.Detection.Type} = {weapon1.Detection.Identifier}\n" +
                              $"#   Ignored: '{source2Name}': {weapon2.Detection.Type} = {weapon2.Detection.Identifier}\n";
                }

                header += "\n";

                File.WriteAllText(filePath, header + yaml);
                Log.Info($"[ConfigLoader] Merged weapons '{source1Name}' + '{source2Name}' into '{outputName}' (by {mergerInfo})");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[ConfigLoader] Failed to merge weapons: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Lookups

        public static SuitDefinition GetSuit(string name)
        {
            return LoadedSuits.TryGetValue(name, out var suit) ? suit : null;
        }

        public static WeaponDefinition GetWeapon(string name)
        {
            return LoadedWeapons.TryGetValue(name, out var weapon) ? weapon : null;
        }

        #endregion
    }

    internal class LegacyConfig
    {
        public bool IsEnabled { get; set; }
        public bool Debug { get; set; }
        public double SuitUpdateInterval { get; set; }
        public Dictionary<string, SuitDefinition> Suits { get; set; }
    }
}