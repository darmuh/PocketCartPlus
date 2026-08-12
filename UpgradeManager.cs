using System.Collections.Generic;

namespace PocketCartPlus
{
    internal class UpgradeManager
    {
        internal static bool LocalItemsUpgrade = false;
        internal static int CartItemsUpgradeLevel
        {
            get
            {
                int upgrade = 0;

                if (!StatsManager.instance.FetchPlayerUpgrades(PlayerAvatar.instance.steamID).ContainsKey("playerUpgradePocketcartKeepItems"))
                {
                    Plugin.WARNING("Unable to find upgrade for CartItemsUpgradeLevel! Returning 0!");
                    return upgrade;
                }   
                else
                    return StatsManager.instance.FetchPlayerUpgrades(PlayerAvatar.instance.steamID)["playerUpgradePocketcartKeepItems"];
            }
            set
            {
                StatsManager.instance.DictionaryUpdateValue("playerUpgradePocketcartKeepItems", PlayerAvatar.instance.steamID, value);
            }
            
        }

        //internal static List<int> PlusSizesChosen = [];
        // not used for anything
        internal static List<PocketCartUpgradeSize> PlusSizeCarts { get; set; } = [];
    }
}
