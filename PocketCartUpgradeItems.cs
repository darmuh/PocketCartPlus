using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using REPOLib.Modules;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PocketCartPlus
{
    public class PocketCartUpgradeItems : MonoBehaviour
    {
        internal static bool localItemsUpgrade = false;
        internal static Dictionary<string, int> dictionaryOfClients = [];
        internal static List<string> ClientsUnlocked = [];
        internal static Value valuePreset = null!;
        internal static readonly float basePriceMultiplier = 4f;
        internal static int UpgradeLevel = 0;

        private void Start()
        {
            Plugin.Spam("PocketCartUpgradeItems Start");
            //Item PocketCart Items
        }

        internal static void ResetProgress()
        {
            localItemsUpgrade = false;
            ClientsUnlocked = [];
        }

        internal static void ClientStart()
        {
            Plugin.Spam("ClientStart");

            if (localItemsUpgrade)
                return;

            Plugin.Spam("Asking host if we have this unlock");
            Networking.HostCheck.RaiseEvent(Networking.CartItemsUpgrade, NetworkingEvents.RaiseMasterClient, SendOptions.SendReliable);
        }

        internal static void HostStart()
        {
            Plugin.Spam("HostStart");
            if(ModConfig.KeepItemsUnlockNoUpgrade.Value)
            {
                Plugin.Spam("Host says this upgrade should be unlocked by everyone, regardless of the shop!");
                Networking.HostCheck.RaiseEvent(Networking.CartItemsUpgrade, NetworkingEvents.RaiseAll, SendOptions.SendReliable);
                return;
            }

            Plugin.Spam("Host updating upgrade list from save");
            LoadSave();

            if(ModConfig.CartItemsUpgradeShared.Value && GameDirector.instance.PlayerList.Any(p => ClientsUnlocked.Contains(p.steamID)))
            {
                Plugin.Spam("Shared upgrades are enabled, telling all clients to enable upgrade!");
                Networking.HostCheck.RaiseEvent(Networking.CartItemsUpgrade, NetworkingEvents.RaiseAll, SendOptions.SendReliable);
            }
        }

        internal static void LoadSave()
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;

            if (!ES3.KeyExists("PocketCartUpgrades_ItemsUpgrade", StatsManager.instance.saveFileCurrent))
            {
                Plugin.Spam("Creating PocketCartUpgrades_ItemsUpgrade for the first time.");
                ES3.Save("PocketCartUpgrades_ItemsUpgrade", ClientsUnlocked, StatsManager.instance.saveFileCurrent);
            }
            else
            {
                ClientsUnlocked = ES3.Load<List<string>>("PocketCartUpgrades_ItemsUpgrade", StatsManager.instance.saveFileCurrent);
                Plugin.Spam("Existing save key loaded");
                Plugin.Spam($"ClientsUnlocked List:\n");
                ClientsUnlocked.Do(c => Plugin.Spam(c));
                UpdateDictionaryRefs();
                Plugin.Spam("--- End of ClientsUnlocked List ---");
                List<int> targetPlayers = [];
                PhotonNetwork.PlayerList.DoIf(p => ClientsUnlocked.Any(c => c == p.UserId), p => targetPlayers.Add(p.ActorNumber));

                RaiseEventOptions custom = new()
                {
                    TargetActors = [.. targetPlayers]
                };

                Plugin.Spam($"Host has detected {targetPlayers.Count} client(s) as having {Networking.CartItemsUpgrade} unlocked. Telling each client to enable behavior");
                Networking.UnlockUpgrade.RaiseEvent(Networking.CartItemsUpgrade, custom, SendOptions.SendReliable);
            }
        }

        internal static void UpdateSave()
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;

            Plugin.Spam("Updating PocketCartUpgrades_ItemsUpgrade for the first time.");
            ES3.Save("PocketCartUpgrades_ItemsUpgrade", ClientsUnlocked, StatsManager.instance.saveFileCurrent);

            UpdateDictionaryRefs();
        }

        private static void UpdateDictionaryRefs()
        {
            //Remove potential nulls
            ClientsUnlocked.RemoveAll(c => c == null);

            ClientsUnlocked.Do(c =>
            {
                if(dictionaryOfClients.Count == 0)
                    dictionaryOfClients.Add(c, 1);
                else if (!dictionaryOfClients.ContainsKey(c))
                    dictionaryOfClients.Add(c, 1);
                else
                    dictionaryOfClients[c] = 1;
            });

            if (!StatsManager.instance.dictionaryOfDictionaries.ContainsKey("playerUpgradePocketcartKeepItems"))
                StatsManager.instance.dictionaryOfDictionaries.Add("playerUpgradePocketcartKeepItems", dictionaryOfClients);
            else
                StatsManager.instance.dictionaryOfDictionaries["playerUpgradePocketcartKeepItems"] = dictionaryOfClients;
        }

        public void Upgrade()
        {
            Plugin.Spam("Pocket Cart items upgrade enabled!");

            Networking.UnlockUpgrade.RaiseEvent(Networking.CartItemsUpgrade, NetworkingEvents.RaiseMasterClient, SendOptions.SendReliable);
        }

        internal static void AskHost(EventData eventData, string steamID)
        {
            Plugin.Spam($"Host checking if client [ {steamID} ] has unlocked {Networking.CartItemsUpgrade}");
            if (ClientsUnlocked.Contains(steamID))
            {
                SendTargetStatus(eventData, steamID);
                return;
            }

            if (ModConfig.CartItemsUpgradeShared.Value && ClientsUnlocked.Count > 0)
            {
                Plugin.Spam("SharedUnlocks detected! Telling all clients this is unlocked!");
                Networking.HostCheck.RaiseEvent(Networking.CartItemsUpgrade, NetworkingEvents.RaiseOthers, SendOptions.SendReliable);
                return;
            }
                
            Plugin.Spam($"client [ {steamID} ] has NOT unlocked {Networking.CartItemsUpgrade}");
        }

        internal static void FromHost()
        {
            Plugin.Spam($"Client received message from Host to unlock {Networking.CartItemsUpgrade}");
            localItemsUpgrade = true;
        }

        internal static void SendTargetStatus(EventData eventData, string steamID)
        {
            RaiseEventOptions custom = new()
            {
                TargetActors = [eventData.Sender]
            };

            Plugin.Spam($"Host has detected client as having {Networking.CartItemsUpgrade} unlocked. Telling client [ {steamID} ] to enable behavior");
            Networking.HostCheck.RaiseEvent(Networking.CartItemsUpgrade, custom, SendOptions.SendReliable);
        }

        internal static void UpdateStatus(EventData eventData)
        {
            //This is a host only event
            if (!SemiFunc.IsMultiplayer())
            {
                localItemsUpgrade = true;
                return;
            }

            //Save stuff
            string steamID;
            if (eventData.Sender == PhotonNetwork.LocalPlayer.ActorNumber)
                steamID = PlayerAvatar.instance.steamID;
            else
            {
                Player client = PhotonNetwork.PlayerList.FirstOrDefault(p => p.ActorNumber == eventData.Sender);
                steamID = client.UserId;
            }
                
            if(steamID == null)
                Plugin.WARNING($"NRE steamID from [ {eventData.Sender} ]");
            else
            {
                if (!ClientsUnlocked.Contains(steamID))
                    ClientsUnlocked.Add(steamID);

                UpdateSave();
            }

            if (ModConfig.CartItemsUpgradeShared.Value)
            {
                localItemsUpgrade = true;
                Networking.HostCheck.RaiseEvent(Networking.CartItemsUpgrade, NetworkingEvents.RaiseOthers, SendOptions.SendReliable);
                Plugin.Spam("This item has shared upgrades, telling all players they have it now");
                Plugin.Spam($"Item was unlocked by [ {steamID} ]");
                return;
            }

            if (eventData.Sender == PhotonNetwork.LocalPlayer.ActorNumber)
                FromHost();
            else
                SendTargetStatus(eventData, steamID);
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
    }
}
