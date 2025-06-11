using System.Collections.Generic;

namespace PocketCartPlus
{
    internal class UpgradeManager
    {
        internal static bool LocalItemsUpgrade = false;
        internal static int CartItemsUpgradeLevel = 0;
        //internal static List<int> PlusSizesChosen = [];
        internal static List<PocketCartUpgradeSize> PlusSizeCarts = [];
    }
}
