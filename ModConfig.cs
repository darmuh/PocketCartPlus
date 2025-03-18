using BepInEx.Configuration;

namespace PocketCartPlus
{
    internal static class ModConfig
    {
        internal static ConfigEntry<bool> DeveloperLogging = null!;

        //Keep Items Upgrade
        internal static ConfigEntry<bool> KeepItemsUnlockNoUpgrade = null!;
        internal static ConfigEntry<float> CartItemsMinPrice = null!;
        internal static ConfigEntry<float> CartItemsMaxPrice = null!;
        internal static ConfigEntry<bool> CartItemsUpgradeShared = null!;
        internal static ConfigEntry<bool> IgnoreEnemies = null!;
        internal static ConfigEntry<bool> PlayerSafetyCheck = null!;
        internal static ConfigEntry<float> CartStabilizationTimer = null!;
        internal static ConfigEntry<float> ItemSafetyTimer = null!;
        
        internal static void Init()
        {
            DeveloperLogging = Plugin.instance.Config.Bind("Debug", "Developer Logging", false, new ConfigDescription("Enable this to see developer logging output"));

            KeepItemsUnlockNoUpgrade = Plugin.instance.Config.Bind("Keep Items Upgrade", "Unlock without Upgrade", false, "Enable this if you want the upgrade enabled without having to buy it from the shop.");
            CartItemsMinPrice = Plugin.instance.Config.Bind("Keep Items Upgrade", "Minimum Price", 6000f, new ConfigDescription("Set this as the minimum base price for this item (before multipliers)", new AcceptableValueRange<float>(100f, 90000f)));
            CartItemsMaxPrice = Plugin.instance.Config.Bind("Keep Items Upgrade", "Maximum Price", 11000f, new ConfigDescription("Set this as the maximum base price for this item (before multipliers)", new AcceptableValueRange<float>(100f, 90000f)));
            CartItemsUpgradeShared = Plugin.instance.Config.Bind("Keep Items Upgrade", "Shared Unlock", false, "Enable this if you want one purchase of this upgrade to unlock the feature for all players in the lobby");
            IgnoreEnemies = Plugin.instance.Config.Bind("Keep Items Upgrade", "Ignore Enemies", false, "Enable this if you do not want the pocket cart to store enemies when this upgrade is enabled.");
            PlayerSafetyCheck = Plugin.instance.Config.Bind("Keep Items Upgrade", "Player Safety Check", true, "When enabled, ensures living players are ignored during pocket cart equip to prevent player death.");
            CartStabilizationTimer = Plugin.instance.Config.Bind("Keep Items Upgrade", "Cart Stabilization Timer", 0.15f, new ConfigDescription("Set this as the amount of time the cart items remain frozen while waiting for the cart to stabilize after equipping.", new AcceptableValueRange<float>(0.05f, 1f)));
            ItemSafetyTimer = Plugin.instance.Config.Bind("Keep Items Upgrade", "Item Safety Timer", 0.2f, new ConfigDescription("Set this as the amount of time the cart items are invulnerable to damage after the cart is re-equipped.", new AcceptableValueRange<float>(0.05f, 1f)));

        }
    }
}
