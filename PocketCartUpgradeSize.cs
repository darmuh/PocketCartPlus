using UnityEngine;

namespace PocketCartPlus
{
    public class PocketCartUpgradeSize : MonoBehaviour
    {
        internal static bool localSizeUpgrade = false;

        private void Start()
        {
            localSizeUpgrade = false;
        }

        public void Upgrade()
        {
            localSizeUpgrade = true;
        }
    }
}
