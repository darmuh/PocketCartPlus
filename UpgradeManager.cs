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

                if (!StatsManager.instance.FetchPlayerUpgrades(PlayerAvatar.instance.steamID).ContainsKey("Pocketcart Keep Items"))
                {
                    Plugin.WARNING("Unable to find upgrade for CartItemsUpgradeLevel! Returning 0!");
                    return upgrade;
                }
                    
                else
                    return StatsManager.instance.FetchPlayerUpgrades(PlayerAvatar.instance.steamID)["Pocketcart Keep Items"];
            }
            set
            {
                StatsManager.instance.DictionaryUpdateValue("playerUpgradePocketcartKeepItems", PlayerAvatar.instance.steamID, value);
            }
            
        }

        //internal static List<int> PlusSizesChosen = [];
        internal static List<PocketCartUpgradeSize> PlusSizeCarts = [];
    }
}
