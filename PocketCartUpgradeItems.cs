using HarmonyLib;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PocketCartPlus.UpgradeManager;

namespace PocketCartPlus
{
    public class PocketCartUpgradeItems : MonoBehaviour
    {
        internal static PocketCartUpgradeItems instance;
        internal PhotonView photonView = null!;
        internal ItemToggle itemToggle = null!;
        internal Item itemComponent = null!;
        internal MapCustom mapCustom = null!;

        //static
        //internal static bool SaveLoaded = false;
        internal static Dictionary<string, int> ClientsUpgradeDictionary = [];
        internal static List<string> ClientsUnlockedOLD = [];
        internal static Value valuePreset = null!;
        internal static readonly float basePriceMultiplier = 4f;

        internal static readonly Color blueUpgradeicon = new(6f/255f, 57f/255f, 112f/255f);
        internal static readonly Color transparent = new(1f, 1f, 1f, 0f);

        private void Start()
        {
            instance = this;
            photonView = gameObject.GetComponent<PhotonView>();
            itemComponent = gameObject.GetComponent<Item>();
            itemToggle = gameObject.GetComponent<ItemToggle>();
            mapCustom = gameObject.GetComponent<MapCustom>();
            Plugin.Spam("PocketCartUpgradeItems Start");
            //Item PocketCart Items

            if(mapCustom != null)
            {
                Plugin.Spam("got mapCustom!");
                if (!ModConfig.CartItemSprite.Value)
                    mapCustom.color = transparent;
                else
                    mapCustom.color = blueUpgradeicon;
            }
        }

        internal static void ClientsUnlocked()
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;

            int clientsUnlocked = 0;
            int sharedLevel = 0;

            GameDirector.instance.PlayerList.Do(p =>
            {
                if (!ClientsUpgradeDictionary.TryGetValue(p.steamID, out int value))
                {
                    Plugin.Spam($"{p.playerName} does not exist in listing");
                    return;
                }

                if (value < 1)
                {
                    Plugin.Spam($"{p.playerName} does not have this unlock!");
                    return;
                }

                //update number of clients who have unlocked this upgrade
                clientsUnlocked++;

                //update upgrade level to highest of clients who have it unlocked
                if (sharedLevel < value)
                    sharedLevel = value;

                //no need to target individual player
                if (HostValues.ShareKeepUpgrade.Value)
                    return;

                Plugin.Spam($"Host has detected {p.playerName} as having CartItemsUpgrade unlocked. Telling client to enable behavior with level {value}");

                if (p.isLocal)
                    GlobalNetworking.Instance.ReceiveItemsUpgrade(value);
                else
                    GlobalNetworking.Instance.photonView.RPC("ReceiveItemsUpgrade", p.photonView.Owner, value);
                //Networking.UnlockUpgrade.RaiseEvent(Networking.CartItemsUpgrade + $":{value}", custom, SendOptions.SendReliable);
            });

            if (!HostValues.ShareKeepUpgrade.Value)
                return;

            if (clientsUnlocked > 0)
            {
                Plugin.Spam($"Host has shared upgrades enabled and detected [ {clientsUnlocked} ] with the upgrade unlocked. Highest upgrade level set to [ {sharedLevel} ]");
                GlobalNetworking.Instance.photonView.RPC("ReceiveItemsUpgrade", RpcTarget.OthersBuffered, sharedLevel);
            }
        }

        internal static void InitDictionary()
        {
            Plugin.Spam("Creating dictionary listing");
            ClientsUpgradeDictionary = [];
            
            if (!PhotonNetwork.IsMasterClient)
                return;

            GameDirector.instance.PlayerList.Do(p =>
            {
                if (p == null)
                    return;

                if (p.steamID == null)
                    return;

                if (!ClientsUpgradeDictionary.ContainsKey(p.steamID))
                    ClientsUpgradeDictionary.Add(p.steamID, 0);
            });
        }

        internal static void ResetProgress()
        {
            Plugin.Message($"UpgradeItems progress reset, will be refreshed by next save");
            CartManager.CartsStoringItems = 0;
            LocalItemsUpgrade = false;
            ClientsUpgradeDictionary = [];
        }

        internal static void LoadStart()
        {
            //convert old save data
            LoadSave();
            Plugin.Spam("--- Start of Clients Unlocked List ---");
            ClientsUpgradeDictionary.Do(d => Plugin.Spam($"{d.Key}"));
            Plugin.Spam("--- End of ClientsUnlocked List ---");
            ClientsUnlocked();
        }

        internal static void LoadSave()
        {
            Plugin.Spam("Loading unlocked clients listing from statsmanager!");
            if (!StatsManager.instance.dictionaryOfDictionaries.TryGetValue("playerUpgradePocketcartKeepItems", out ClientsUpgradeDictionary))
                Plugin.WARNING("Unable to load save key!");

            //Get old save key info
            GetOldSaveData();
        }

        internal void UpdateSave()
        {
            //if (!SemiFunc.IsMasterClientOrSingleplayer())
                //return;

            Plugin.Spam("Updating PocketCartUpgrades_ItemsUpgrade in dictionary!");
            StatsManager.instance.dictionaryOfDictionaries["playerUpgradePocketcartKeepItems"] = ClientsUpgradeDictionary;
        }

        private static void GetOldSaveData()
        {
            if (ES3.KeyExists("PocketCartUpgrades_ItemsUpgrade", StatsManager.instance.saveFileCurrent))
            {
                Plugin.Spam("Old existing save key found! Loading values");
                ClientsUnlockedOLD = ES3.Load<List<string>>("PocketCartUpgrades_ItemsUpgrade", StatsManager.instance.saveFileCurrent);
                if (ClientsUnlockedOLD.Count > 0)
                {
                    ClientsUnlockedOLD.RemoveAll(c => c == null);
                    ClientsUnlockedOLD.Do(c =>
                    {
                        if (!ClientsUpgradeDictionary.ContainsKey(c))
                            ClientsUpgradeDictionary.Add(c, 1);
                        else
                        {
                            if (ClientsUpgradeDictionary[c] == 0)
                                ClientsUpgradeDictionary[c] = 1;
                        }
                    });
                }

                ES3.DeleteKey("PocketCartUpgrades_ItemsUpgrade");
                ClientsUnlockedOLD = [];
            }
        }

        public void Upgrade()
        {
            Plugin.Spam("PocketCartUpgradeItems Upgrade!");
            PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarGetFromPhotonID(itemToggle.playerTogglePhotonID);
            if (playerAvatar.isLocal)
            {
                if (!LocalItemsUpgrade)
                    LocalItemsUpgrade = true;
            }

            if (!ClientsUpgradeDictionary.TryGetValue(playerAvatar.steamID, out int upgradeLevel))
            {
                Plugin.Spam($"Unable to find [ {playerAvatar.steamID} ] in ClientsUpgradeDictionary, creating new entry at level 1!");
                ClientsUpgradeDictionary.Add(playerAvatar.steamID, 1);
            }
            else
                ClientsUpgradeDictionary[playerAvatar.steamID]++;

            UpdateSave();

            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;

            if (HostValues.ShareKeepUpgrade.Value)
            {
                Plugin.Spam("Sending upgrade status to all other clients!");

                photonView.RPC("ReceiveUpgrade", RpcTarget.OthersBuffered, CartItemsUpgradeLevel);
            }
        }

        [PunRPC]
        private void ReceiveUpgrade(int upgradeLevel)
        {
            //this is a client only RPC
            if (PhotonNetwork.IsMasterClient)
                return;

            if (!LocalItemsUpgrade)
                LocalItemsUpgrade = true;

            if (upgradeLevel != CartItemsUpgradeLevel)
                CartItemsUpgradeLevel = upgradeLevel;
        }

        internal static void ValueRef()
        {
            if (valuePreset == null)
                valuePreset = ScriptableObject.CreateInstance<Value>();

            valuePreset.valueMin = HostValues.KeepMinPrice.Value / basePriceMultiplier;
            valuePreset.valueMax = HostValues.KeepMaxPrice.Value / basePriceMultiplier;
            valuePreset.name = "pocketcart_keepitems";

            Plugin.Spam($"valuePreset created for keepItems upgrade with base min price of {HostValues.KeepMinPrice.Value} and base max price of {HostValues.KeepMaxPrice.Value}");
        }

        internal static void ShopPatch()
        {
            bool shouldAdd = false;

            Item keepItems = REPOLib.Modules.Items.GetItemByName("Item PocketCart Items");

            if (keepItems == null)
            {
                Plugin.Spam($"Item not found!");
                return;
            }

            if (HostValues.KeepItemsRarity.Value >= Plugin.Rand.Next(0, 100))
                shouldAdd = true;

            if (!shouldAdd && ShopManager.instance.potentialItemUpgrades.Contains(keepItems))
            {
                int CountToReplace = ShopManager.instance.potentialItems.Count(i => i.itemAssetName == keepItems.itemAssetName);
                Plugin.Spam($"Add-on rarity has determined {keepItems.itemName} should be removed from the store! Original contains {CountToReplace} of this item");
                ShopManager.instance.potentialItemUpgrades.RemoveAll(i => i.itemAssetName == keepItems.itemAssetName);

                if (CountToReplace > 0 && ShopManager.instance.potentialItemUpgrades.Count > 0)
                {
                    for (int i = 0; i < CountToReplace; i++)
                    {
                        ShopManager.instance.potentialItemUpgrades.Add(ShopManager.instance.potentialItemUpgrades[Plugin.Rand.Next(0, ShopManager.instance.potentialItemUpgrades.Count)]);
                        Plugin.Spam("Replaced upgrade with another random valid upgrade");
                    }
                }
            }
                

            Plugin.Spam($"Rarity determined item is a valid potential itemUpgrade in the shop {shouldAdd}");
            keepItems.value = valuePreset;
            Plugin.Spam($"Value preset set for KeepPocketCartItems Upgrade!");
        }
    }
}
