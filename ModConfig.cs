using BepInEx.Configuration;

namespace PocketCartPlus
{
    internal static class ModConfig
    {
        internal static ConfigEntry<bool> DeveloperLogging { get; private set; } = null!;

        //Keep Items Upgrade
        internal static ConfigEntry<bool> KeepItemsUnlockNoUpgrade { get; private set; } = null!;
        internal static ConfigEntry<float> CartItemsMinPrice { get; private set; } = null!;
        internal static ConfigEntry<float> CartItemsMaxPrice { get; private set; } = null!;
        internal static ConfigEntry<bool> CartItemsUpgradeShared { get; private set; } = null!;
        internal static ConfigEntry<bool> IgnoreEnemies { get; private set; } = null!;
        internal static ConfigEntry<bool> PlayerSafetyCheck { get; private set; } = null!;
        internal static ConfigEntry<float> CartStabilizationTimer { get; private set; } = null!;
        internal static ConfigEntry<float> ItemSafetyTimer { get; private set; } = null!;
        internal static ConfigEntry<int> CartItemRarity { get; private set; } = null!;
        internal static ConfigEntry<bool> CartItemSprite { get; private set; } = null!;
        internal static ConfigEntry<bool> CartItemLevels { get; private set; } = null!;
        internal static ConfigEntry<bool> AllowDeposit { get; private set; } = null!;

        //Plus Item
        internal static ConfigEntry<int> PlusItemRarity { get; private set; } = null!;
        internal static ConfigEntry<float> PlusItemMinPrice { get; private set; } = null!;
        internal static ConfigEntry<float> PlusItemMaxPrice { get; private set; } = null!;
        internal static ConfigEntry<bool> RareVariants { get; private set; } = null!;

        //Void Remote
        internal static ConfigEntry<int> VoidRemoteRarity { get; private set; } = null!;
        internal static ConfigEntry<float> VoidRemoteMinPrice { get; private set; } = null!;
        internal static ConfigEntry<float> VoidRemoteMaxPrice { get; private set; } = null!;

        internal static void Init()
        {
            DeveloperLogging = Plugin.instance.Config.Bind("Debug", "Developer Logging", false, new ConfigDescription("Enable this to see developer logging output"));

            KeepItemsUnlockNoUpgrade = Plugin.instance.Config.Bind("Keep Items Upgrade", "Unlock without Upgrade", false, "Enable this if you want the upgrade enabled without having to buy it from the shop.");
            CartItemsMinPrice = Plugin.instance.Config.Bind("Keep Items Upgrade", "Minimum Price", 6000f, new ConfigDescription("Set this as the minimum base price for this item (before multipliers)", new AcceptableValueRange<float>(100f, 90000f)));
            CartItemsMaxPrice = Plugin.instance.Config.Bind("Keep Items Upgrade", "Maximum Price", 11000f, new ConfigDescription("Set this as the maximum base price for this item (before multipliers)", new AcceptableValueRange<float>(100f, 90000f)));
            CartItemRarity = Plugin.instance.Config.Bind("Keep Items Upgrade", "Rarity Percentage (Add-on)", 75, new ConfigDescription("This is a percentage from 0-100 of how rarely this item will be added to the store.\nThis is an added-on rarity on-top of base-game's rng based spawn system.\n Set to 0 to never spawn this upgrade.", new AcceptableValueRange<int>(0, 100)));
            CartItemsUpgradeShared = Plugin.instance.Config.Bind("Keep Items Upgrade", "Shared Unlock", false, "Enable this if you want one purchase of this upgrade to unlock the feature for all players in the lobby.\nThis should work with \"Upgrade Levels\" enabled");
            IgnoreEnemies = Plugin.instance.Config.Bind("Keep Items Upgrade", "Ignore Enemies", true, "Disable this if you'd like the pocket cart to try to store enemies as well as items.\nNote: Storing enemies is still fairly buggy.\nThe recommended setting for this is TRUE if you wish to play with as little jank as possible.");
            PlayerSafetyCheck = Plugin.instance.Config.Bind("Keep Items Upgrade", "Player Safety Check", true, "When enabled, ensures living players are ignored during pocket cart equip to prevent player death.\nDisabling this will result in a more buggy experience but it can be funny.");
            CartItemSprite = Plugin.instance.Config.Bind("Keep Items Upgrade", "Show On MiniMap", true, "Disable this if you wish to hide the icon for this item on the minimap");
            CartItemLevels = Plugin.instance.Config.Bind("Keep Items Upgrade", "Upgrade Levels", true, "When enabled, the upgrade will only apply for as many carts as the current upgrade level.\nSo upgrade level 1 will only store items for one pocket cart at a time");
            CartStabilizationTimer = Plugin.instance.Config.Bind("Keep Items Upgrade", "Cart Stabilization Timer", 0.15f, new ConfigDescription("Set this as the amount of time the cart items remain frozen while waiting for the cart to stabilize after equipping.", new AcceptableValueRange<float>(0.05f, 1f)));
            ItemSafetyTimer = Plugin.instance.Config.Bind("Keep Items Upgrade", "Item Safety Timer", 0.2f, new ConfigDescription("Set this as the amount of time the cart items are invulnerable to damage after the cart is re-equipped.", new AcceptableValueRange<float>(0.05f, 1f)));
            AllowDeposit = Plugin.instance.Config.Bind("Keep Items Upgrade", "Allow Deposit", true, "Enable this to stop items from being stored when holding ALT");

            PlusItemRarity = Plugin.instance.Config.Bind("Cart Plus Item", "Rarity Percentage (Add-on)", 95, new ConfigDescription("This is a percentage from 0-100 of how rarely this item will be added to the store.\nThis is an added-on rarity on-top of base-game's rng based spawn system.\n Set to 0 to never spawn this item.", new AcceptableValueRange<int>(0, 100)));
            PlusItemMinPrice = Plugin.instance.Config.Bind("Cart Plus Item", "Minimum Price", 30000f, new ConfigDescription("Set this as the minimum base price for this item (before multipliers)", new AcceptableValueRange<float>(100f, 90000f)));
            PlusItemMaxPrice = Plugin.instance.Config.Bind("Cart Plus Item", "Maximum Price", 60000f, new ConfigDescription("Set this as the maximum base price for this item (before multipliers)", new AcceptableValueRange<float>(100f, 90000f)));
            RareVariants = Plugin.instance.Config.Bind("Cart Plus Item", "Rare Variants", true, "When enabled, the pocket cart plus has a rare chance to spawn as either the PLUS2 (150% scale) or PLUS3 (175% scale)");

            VoidRemoteRarity = Plugin.instance.Config.Bind("Void Remote Item", "Rarity Percentage (Add-on)", 100, new ConfigDescription("This is a percentage from 0-100 of how rarely this item will be added to the store.\nThis is an added-on rarity on-top of base-game's rng based spawn system.\n Set to 0 to never spawn this item.", new AcceptableValueRange<int>(0, 100)));
            VoidRemoteMinPrice = Plugin.instance.Config.Bind("Void Remote Item", "Minimum Price", 100f, new ConfigDescription("Set this as the minimum base price for this item (before multipliers)", new AcceptableValueRange<float>(100f, 90000f)));
            VoidRemoteMaxPrice = Plugin.instance.Config.Bind("Void Remote Item", "Maximum Price", 2500f, new ConfigDescription("Set this as the maximum base price for this item (before multipliers)", new AcceptableValueRange<float>(100f, 90000f)));
        }
    }
}
