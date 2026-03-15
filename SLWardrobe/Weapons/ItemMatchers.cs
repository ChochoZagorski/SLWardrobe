using System;
#if EXILED
using Exiled.API.Features;
using Exiled.API.Features.Items;
#else
using LabApi.Features.Wrappers;
using Log = LabApi.Features.Console.Logger;
#endif
using SLWardrobe.Models;

namespace SLWardrobe.Weapons
{
    public class VanillaItemMatcher : IItemMatcher
    {
        private readonly ItemType targetType;

        public string Description => $"Vanilla: {targetType}";

        public VanillaItemMatcher(string itemTypeName)
        {
            if (!Enum.TryParse<ItemType>(itemTypeName, true, out targetType))
            {
                Log.Warn($"[VanillaItemMatcher] Unknown item type: {itemTypeName}");
                targetType = ItemType.None;
            }
        }

        public bool Matches(Item item, Player owner)
        {
            return item != null && item.Type == targetType;
        }
    }

#if EXILED
    public class CustomItemMatcher : IItemMatcher
    {
        private readonly string identifier;
        private readonly string source;

        public string Description => string.IsNullOrEmpty(source)
            ? $"Custom: {identifier}"
            : $"Custom: {identifier} ({source})";

        public CustomItemMatcher(string identifier, string source = "")
        {
            this.identifier = identifier;
            this.source = source;
        }

        public bool Matches(Item item, Player owner)
        {
            if (item == null) return false;

            try
            {
                if (Exiled.CustomItems.API.Features.CustomItem.TryGet(item, out var ci) && ci != null)
                {
                    return ci.Name.Equals(identifier, StringComparison.OrdinalIgnoreCase) ||
                           ci.Id.ToString() == identifier;
                }
            }
            catch
            {
            }

            return false;
        }
    }
#endif

    public static class ItemMatcherFactory
    {
        public static IItemMatcher Create(ItemDetection detection)
        {
            switch (detection.Type?.ToLower())
            {
                case "vanillaitem":
                case "vanilla":
                    return new VanillaItemMatcher(detection.Identifier);
#if EXILED
                case "customitem":
                case "custom":
                    return new CustomItemMatcher(detection.Identifier, detection.CustomItemSource);
#endif
                default:
                    Log.Warn($"[ItemMatcherFactory] Unknown detection type '{detection.Type}', defaulting to VanillaItem");
                    return new VanillaItemMatcher(detection.Identifier);
            }
        }
    }
}