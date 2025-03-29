using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PocketCartPlus.UpgradeManager;

namespace PocketCartPlus
{
    public class PocketCartUpgradeItems : MonoBehaviour
    {
        internal PhotonView photonView = null!;
        internal ItemToggle itemToggle = null!;
        internal Item itemComponent = null!;
        internal MapCustom mapCustom = null!;

        //static
        internal static bool SaveLoaded = false;
        internal static Dictionary<string, int> dictionaryOfClients = [];
        internal static List<string> ClientsUnlockedOLD = [];
        internal static Value valuePreset = null!;
        internal static readonly float basePriceMultiplier = 4f;

        internal static readonly Color blueUpgradeicon = new(6f/255f, 57f/255f, 112f/255f);
        internal static readonly Color transparent = new(1f, 1f, 1f, 0f);

        private void Start()
        {
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

            if (!SaveLoaded)
            {
                LoadStart();
                SaveLoaded = true;
            }
        }

        internal static void InitDictionary()
        {
            Plugin.Spam("Creating dictionary listing");
            dictionaryOfClients = [];
            
            if (!PhotonNetwork.IsMasterClient)
                return;

            GameDirector.instance.PlayerList.Do(p =>
            {
                if (p == null)
                    return;

                if (p.steamID == null)
                    return;

                if (!dictionaryOfClients.ContainsKey(p.steamID))
                    dictionaryOfClients.Add(p.steamID, 0);
            });
        }

        internal static void ResetProgress()
        {
            SaveLoaded = false;
            CartManager.CartsStoringItems = 0;
            CartItemsUpgradeLevel = 0;
            LocalItemsUpgrade = false;
            dictionaryOfClients = [];
        }

        internal void LoadStart()
        {
            //convert old save data
            LoadSave();
            Plugin.Spam("--- Start of Clients Unlocked List ---");
            dictionaryOfClients.Do(d => Plugin.Spam($"{d.Key}"));
            Plugin.Spam("--- End of ClientsUnlocked List ---");

            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;

            int clientsUnlocked = 0;
            int sharedLevel = 0;

            GameDirector.instance.PlayerList.Do(p =>
            {
                if(!dictionaryOfClients.TryGetValue(p.steamID, out int value))
                {
                    Plugin.Spam($"{p.playerName} does not exist in listing");
                    return;
                }

                if(value < 1)
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
                if (ModConfig.CartItemsUpgradeShared.Value)
                    return;

                Plugin.Spam($"Host has detected {p.playerName} as having CartItemsUpgrade unlocked. Telling client to enable behavior with level {value}");

                photonView.RPC("ReceiveUpgrade", p.photonView.Owner, value);
                //Networking.UnlockUpgrade.RaiseEvent(Networking.CartItemsUpgrade + $":{value}", custom, SendOptions.SendReliable);
            });

            if (!ModConfig.CartItemsUpgradeShared.Value)
                return;

            if(clientsUnlocked > 0)
            {
                Plugin.Spam($"Host has shared upgrades enabled and detected [ {clientsUnlocked} ] with the upgrade unlocked. Highest upgrade level set to [ {sharedLevel} ]");
                photonView.RPC("ReceiveUpgrade", RpcTarget.Others, sharedLevel);
            } 
        }

        internal void LoadSave()
        {
            Plugin.Spam("Loading unlocked clients listing from statsmanager!");
            if (!StatsManager.instance.dictionaryOfDictionaries.TryGetValue("playerUpgradePocketcartKeepItems", out dictionaryOfClients))
                Plugin.WARNING("Unable to load save key!");

            //Get old save key info
            GetOldSaveData();
        }

        internal void UpdateSave()
        {
            //if (!SemiFunc.IsMasterClientOrSingleplayer())
                //return;

            Plugin.Spam("Updating PocketCartUpgrades_ItemsUpgrade in dictionary!");
            StatsManager.instance.dictionaryOfDictionaries["playerUpgradePocketcartKeepItems"] = dictionaryOfClients;
        }

        private void GetOldSaveData()
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
                        if (!dictionaryOfClients.ContainsKey(c))
                            dictionaryOfClients.Add(c, 1);
                        else
                        {
                            if (dictionaryOfClients[c] == 0)
                                dictionaryOfClients[c] = 1;
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

                CartItemsUpgradeLevel++;

                Plugin.Spam($"Enabling PocketCart Keep Items Upgrade for local player! Level [ {CartItemsUpgradeLevel} ]");
            }

            if (!dictionaryOfClients.TryGetValue(playerAvatar.steamID, out int upgradeLevel))
            {
                Plugin.WARNING($"Unable to find [ {playerAvatar.steamID} ] in dictionaryOfClients, creating new entry at level 1!");
                dictionaryOfClients.Add(playerAvatar.steamID, 1);
            }
            else
                dictionaryOfClients[playerAvatar.steamID]++;

            UpdateSave();

            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;

            if (ModConfig.CartItemsUpgradeShared.Value)
            {
                Plugin.Spam("Sending upgrade status to all other clients!");
                List<Player> allOthers = [.. PhotonNetwork.PlayerListOthers];
                if (allOthers.Contains(playerAvatar.photonView.Owner))
                    allOthers.Remove(playerAvatar.photonView.Owner);

                allOthers.Do(o =>
                {
                    photonView.RPC("ReceiveUpgrade", o, CartItemsUpgradeLevel);
                });
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

            if (upgradeLevel > CartItemsUpgradeLevel)
                CartItemsUpgradeLevel = upgradeLevel;
        }

        internal static void ValueRef()
        {
            if (valuePreset == null)
                valuePreset = ScriptableObject.CreateInstance<Value>();

            valuePreset.valueMin = ModConfig.CartItemsMinPrice.Value / basePriceMultiplier;
            valuePreset.valueMax = ModConfig.CartItemsMaxPrice.Value / basePriceMultiplier;
            valuePreset.name = "pocketcart_keepitems";

            Plugin.Spam($"valuePreset created for keepItems upgrade with base min price of {ModConfig.CartItemsMinPrice.Value} and base max price of {ModConfig.CartItemsMaxPrice.Value}");
        }

        internal static void ShopPatch()
        {
            bool shouldAdd = false;

            Item keepItems = ShopManager.instance.potentialItemUpgrades.FirstOrDefault(i => i.itemAssetName == "Item PocketCart Items");

            if (keepItems == null)
            {
                Plugin.Spam($"Item not found in potentialItemUpgrades ({ShopManager.instance.potentialItemUpgrades.Count})!");
                return;
            }

            if (ModConfig.CartItemRarity.Value >= Plugin.Rand.Next(0, 100))
                shouldAdd = true;

            if (!shouldAdd)
                ShopManager.instance.potentialItemUpgrades.Remove(keepItems);

            Plugin.Spam($"Rarity determined item is a valid potential itemUpgrade in the shop {shouldAdd}");
            keepItems.value = valuePreset;
            Plugin.Spam($"Value preset set for KeepPocketCartItems Upgrade!");
        }
    }
}
