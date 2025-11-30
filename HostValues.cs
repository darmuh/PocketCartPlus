using BepInEx.Configuration;
using HarmonyLib;
using Photon.Pun;
using static PocketCartPlus.ModConfig;

namespace PocketCartPlus
{
    internal class HostValues
    {
        internal static HostConfigItem<bool> KeepNoUpgrade { get; private set; } = new(KeepItemsUnlockNoUpgrade);
        internal static HostConfigItem<bool> KeepIgnoreEnemies { get; private set; } = new(IgnoreEnemies);
        internal static HostConfigItem<bool> PlayerSafety { get; private set; } = new(PlayerSafetyCheck);
        internal static HostConfigItem<float> StabilzeTimer { get; private set; } = new(CartStabilizationTimer);
        internal static HostConfigItem<float> SafetyTimer { get; private set; } = new(ItemSafetyTimer);
        internal static HostConfigItem<bool> KeepItemsLevels { get; private set; } = new(CartItemLevels);


        // --------------- No Sync required
        internal static HostConfigItem<bool> ShareKeepUpgrade { get; private set; } = new(CartItemsUpgradeShared, false);
        internal static HostConfigItem<float> KeepMinPrice { get; private set; } = new(CartItemsMinPrice, false);
        internal static HostConfigItem<float> KeepMaxPrice { get; private set; } = new(CartItemsMaxPrice, false);
        internal static HostConfigItem<int> KeepItemsRarity { get; private set; } = new(CartItemRarity, false);
        internal static HostConfigItem<int> PlusCartRarity { get; private set; } = new(PlusItemRarity, false);
        internal static HostConfigItem<float> PlusCartMinPrice { get; private set; } = new(PlusItemMinPrice, false);
        internal static HostConfigItem<float> PlusCartMaxPrice { get; private set; } = new(PlusItemMaxPrice, false);
        internal static HostConfigItem<bool> PlusCartRareVariants { get; private set; } = new(RareVariants, false);
        internal static HostConfigItem<int> VRRarity { get; private set; } = new(VoidRemoteRarity, false);
        internal static HostConfigItem<float> VRMinPrice { get; private set; } = new(VoidRemoteMinPrice, false);
        internal static HostConfigItem<float> VRMaxPrice { get; private set; } = new(VoidRemoteMaxPrice, false);

        internal static void StartGame()
        {
            Plugin.Spam("Checking config items!!");
            HostConfigBase.namesToSync = [];
            HostConfigBase.HostConfigItems.Do(p =>
            {
                if (p is IValueSetter setter)
                    setter.SetValue(p.RequireSync);
            });

            if (GlobalNetworking.Instance == null && GameManager.Multiplayer())
                Plugin.WARNING("GlobalNetworking is null!");

            HostConfigBase.SyncIsReady(); //only host runs this
            HostConfigBase.HostConfigInit = true;
        }

        internal static void UpdateValue(ConfigEntryBase configItem)
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            Plugin.Spam($"Host updated value for {configItem.Definition.Key}");
            HostConfigBase valBase = HostConfigBase.HostConfigItems.Find(c => c.Name == configItem.Definition.Key);
            
            if (valBase == null)
            {
                Plugin.Spam($"{configItem.Definition.Key} is not a HostValue being watched");
                return;
            }

            GlobalNetworking.Instance.HostSendIndividual(configItem.Definition.Key, configItem.BoxedValue);
            GlobalNetworking.Instance.photonView.RPC("HostSendIndividual", RpcTarget.OthersBuffered, configItem.Definition.Key, configItem.BoxedValue);
        }
    }
}
