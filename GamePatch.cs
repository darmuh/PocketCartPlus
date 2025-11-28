using HarmonyLib;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PocketCartPlus
{

    //for adding UI hint to deposit items
    [HarmonyPatch(typeof(PhysGrabCart), nameof(PhysGrabCart.StateMessages))]
    public class CartMessagePatch
    {
        internal static Color hintColor = new(220f / 255f, 204f / 255f, 188f / 255f);

        public static void Postfix(PhysGrabCart __instance)
        {

            if(!PlayerAvatar.instance.physGrabber.grabbed && HintUI.instance.grabHint)
                HintUI.instance.grabHint = false;

            if (!__instance.physGrabObject.grabbedLocal || !ModConfig.AllowDeposit.Value)
                return;

            if (!GetPocketCarts.AllSmallCarts.Contains(__instance))
                return;

            if (!HostValues.KeepNoUpgrade.Value && !UpgradeManager.LocalItemsUpgrade)
                return;

            if (!HostValues.KeepNoUpgrade.Value && HostValues.KeepItemsLevels.Value && UpgradeManager.CartItemsUpgradeLevel <= CartManager.CartsStoringItems)
                return;

            if (__instance.currentState != PhysGrabCart.State.Handled && __instance.currentState != PhysGrabCart.State.Dragged)
                return;

            if (SemiFunc.RunIsShop())
                return;  
            

            if(HintUI.instance.transform.parent != HintUI.instance.normalParent)
                HintUI.instance.transform.SetParent(HintUI.instance.normalParent);

            HintUI.instance.grabHint = true;
            HintUI.instance.ShowInfo("Hold <color=#f0bf30>[ALT]</color> to deposit items", HintUI.instance.textColor, 12f);
        }
    }

    [HarmonyPatch(typeof(ItemInfoExtraUI), nameof(ItemInfoExtraUI.Start))]
    public class CreateHintUI
    {
        internal static GameObject Hinter = null!;
        public static void Postfix(ItemInfoExtraUI __instance)
        {
            if (!SpawnPlayerStuff.AreWeInGame())
                return;

            if (HintUI.instance != null || __instance.Text == null)
                return;

            Plugin.Spam("Creating Hinter!");
            Hinter = new("Hinter");
            TextMeshProUGUI text = Hinter.AddComponent<TextMeshProUGUI>();
            text.font = __instance.Text.font;
            text.fontMaterial = __instance.Text.fontMaterial;
            text.fontSharedMaterial = __instance.Text.fontSharedMaterial;
            text.fontSharedMaterials = __instance.Text.fontSharedMaterials;
            text.spriteAsset = __instance.Text.spriteAsset;
            text.alignment = TextAlignmentOptions.Midline;
            text.fontSize = 12f;
            text.fontStyle = FontStyles.SmallCaps;
            //Hinter.AddComponent<RectTransform>();
            Hinter.transform.SetParent(__instance.gameObject.transform);
            Hinter.AddComponent<HintUI>();
        }
    }

    [HarmonyPatch(typeof(SemiFunc), nameof(SemiFunc.OnLevelGenDone))]
    public class PocketDimension
    {
        internal static GameObject ThePocket = null!;
        internal static VoidRef voidRef = null!;
        public static void Postfix()
        {
            if (!SpawnPlayerStuff.AreWeInGame())
                return;

            if(ThePocket == null)
            {
                ThePocket = GameObject.Instantiate(Plugin.PocketDimension, new Vector3(999f, 0, 0), new Quaternion(), LevelGenerator.Instance.LevelParent.transform);
            }

            if(voidRef == null)
            {
                voidRef = ThePocket.GetComponentInChildren<VoidRef>();
                Plugin.Spam($"voidRef is null - {voidRef == null}");
            }

        }
    }

    [HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.SpawnRPC))]
    public class SpawnPlayerStuff
    {
        public static void Postfix(PlayerAvatar __instance)
        {
            if (!AreWeInGame() || !__instance.photonView.IsMine)
                return;

            //Reset CartsStoringItems amount (since they have just spawned and cant possibly be storing items)
            Plugin.Message("Player Spawned: CartsStoringItems set to 0");
            CartManager.CartsStoringItems = 0;
        }

        internal static bool AreWeInGame()
        {
            if (RunManager.instance.levelCurrent == RunManager.instance.levelLobbyMenu)
                return false;

            if (RunManager.instance.levelCurrent == RunManager.instance.levelMainMenu)
                return false;

            return true;
        } 
    }

    [HarmonyPatch(typeof(StatsManager), nameof(StatsManager.LoadGame))]
    public class StatsManagerLoad
    {
        public static void Postfix()
        {
            PocketCartUpgradeItems.LoadStart();
        }
    }

    [HarmonyPatch(typeof(RunManagerPUN), nameof(RunManagerPUN.Start))]
    public class NetworkingInstance
    {
        public static void Postfix()
        {
            if (RunManager.instance.runManagerPUN.gameObject.GetComponent<GlobalNetworking>() == null)
                RunManager.instance.runManagerPUN.gameObject.AddComponent<GlobalNetworking>();

            HostConfigCheck();
        }

        internal static void HostConfigCheck()
        {
            if (!HostConfigBase.HostConfigInit && SemiFunc.IsMasterClientOrSingleplayer() && !SemiFunc.MenuLevel())
                HostValues.StartGame();
        }
    }

    [HarmonyPatch(typeof(StatsManager), nameof(StatsManager.Start))]
    public class StatsManagerStart
    {
        public static void Postfix(StatsManager __instance)
        {
            PocketCartUpgradeItems.InitDictionary();
            Plugin.Spam("Updating statsmanager with our save keys!");
            __instance.dictionaryOfDictionaries.TryAdd("playerUpgradePocketcartKeepItems", PocketCartUpgradeItems.ClientsUpgradeDictionary);
        }
    }

    [HarmonyPatch(typeof(SemiFunc), nameof(SemiFunc.OnSceneSwitch))]
    public class LeaveToMainReset
    {
        public static void Postfix(bool _gameOver, bool _leaveGame)
        {
            if(_gameOver || _leaveGame)
            {
                PocketCartUpgradeItems.ResetProgress();
            }

            NetworkingInstance.HostConfigCheck();

            if (HostConfigBase.HostConfigInit)
                HostConfigBase.HostConfigInit = !_leaveGame;
        }
    }

    [HarmonyPatch(typeof(ShopManager), nameof(ShopManager.GetAllItemsFromStatsManager))]
    public class ModifyItemRarity
    {
        public static void Postfix(ShopManager __instance)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;

            //prices
            Plugin.Log.LogDebug("Value reference patches");
            PocketCartUpgradeItems.ValueRef();
            PocketCartUpgradeSize.ValueRef();
            VoidController.ValueRef();

            //add-on rarities
            Plugin.Log.LogDebug("Add-on rarity patches");
            ShopPatch(HostValues.KeepItemsRarity.Value, "Item PocketCart Items", PocketCartUpgradeItems.valuePreset, ref __instance.potentialItemUpgrades);
            ShopPatch(HostValues.PlusCartRarity.Value, "Item PCartPlus", PocketCartUpgradeSize.valuePreset, ref __instance.potentialItems);
            ShopPatch(HostValues.VRRarity.Value, "Item VoidRemote", VoidController.valuePreset, ref __instance.potentialSecretItems);
        }

        private static void ShopPatch(int config, string prefabName, Value valuePreset, ref List<Item> RefItemList)
        {
            bool shouldAdd = false;

            Item itemName = RefItemList.FirstOrDefault(i => i.prefab.prefabName == prefabName);

            if (itemName == null)
            {
                Plugin.Spam($"Item [{prefabName}] not found!");
                return;
            }

            if (config >= Plugin.Rand.Next(0, 100))
                shouldAdd = true;

            if (!shouldAdd)
            {
                int CountToReplace = RefItemList.Count(i => i.itemName == itemName.itemName);
                Plugin.Spam($"Add-on rarity has determined {itemName.itemName} should be removed from the store! Original contains {CountToReplace} of this item");
                RefItemList.RemoveAll(i => i.itemName == itemName.itemName);

                if (CountToReplace > 0 && RefItemList.Count > 0)
                {
                    for (int i = 0; i < CountToReplace; i++)
                    {
                        RefItemList.Add(RefItemList[Plugin.Rand.Next(0, RefItemList.Count)]);
                        Plugin.Spam("Replaced item with another random item of same type");
                    }
                }
            }

            Plugin.Spam($"Rarity determined Item [{prefabName}] can be added in the shop {shouldAdd}");
            itemName.value = valuePreset;
            Plugin.Spam($"Value preset set for Item [{prefabName}]");
        }

        private static void ShopPatch(int config, string prefabName, Value valuePreset, ref Dictionary<SemiFunc.itemSecretShopType, List<Item>> secretShopDict)
        {
            bool shouldAdd = false;
            Item itemName = null!;
            SemiFunc.itemSecretShopType secretType = SemiFunc.itemSecretShopType.none;

            foreach(var type in secretShopDict.Keys)
            {
                List<Item> list = secretShopDict[type];
                itemName = list.FirstOrDefault(x => x.prefab.prefabName == prefabName);

                if (itemName != null)
                {
                    secretType = type;
                    break;
                }
            }

            if (itemName == null)
            {
                Plugin.Spam($"Item [{prefabName}] not found in secret shop!");
                return;
            }

            int rand = Plugin.Rand.Next(0, 100);

            if (config >= rand)
                shouldAdd = true;
            else
                Plugin.Spam($"Config {config} is less than {rand}");

            if (!shouldAdd)
            {
                int CountToReplace = secretShopDict[secretType].Count(i => i.itemName == itemName.itemName);
                Plugin.Spam($"Add-on rarity has determined {itemName.itemName} should be removed from the store! Original contains {CountToReplace} of this item");
                secretShopDict[secretType].RemoveAll(i => i.itemName == itemName.itemName);

                if (CountToReplace > 0 && secretShopDict.Count > 0)
                {
                    for (int i = 0; i < CountToReplace; i++)
                    {
                        secretShopDict[secretType].Add(secretShopDict[secretType][Plugin.Rand.Next(0, secretShopDict.Count)]);
                        Plugin.Spam("Replaced secret shop item with another random valid secret shop item");
                    }
                }
            }

            Plugin.Spam($"Rarity determined Item [{prefabName}] can be added in the secret shop {shouldAdd}");
            itemName.value = valuePreset;
            Plugin.Spam($"Value preset set for Item [{prefabName}]");
        }
    }

    [HarmonyPatch(typeof(PhysGrabCart), nameof(PhysGrabCart.Start))]
    public class GetPocketCarts
    {
        internal static List<PhysGrabCart> AllSmallCarts = [];
        public static void Postfix(PhysGrabCart __instance)
        {
            if (!__instance.isSmallCart)
                return;

            CartManager cartManager = __instance.gameObject.GetComponent<CartManager>() ?? __instance.gameObject.AddComponent<CartManager>();

            AllSmallCarts.RemoveAll(c => c == null);
            AllSmallCarts.Add(__instance);
        }
    }

    //for pocketcart upgraded size
    [HarmonyPatch(typeof(ItemEquippable), nameof(ItemEquippable.AnimateUnequip))]
    public class FixScaleofPlus
    {
        public static void Postfix(ItemEquippable __instance)
        {
            PocketCartUpgradeSize upgrade = __instance.gameObject.GetComponent<PocketCartUpgradeSize>();
            if (upgrade != null)
            {
                Plugin.Spam("Returning pocketcart plus to original scale!");
                upgrade.ReturnScale();
            }
                
        }
    }

    //need to patch here to parent transform early enough
    [HarmonyPatch(typeof(ItemEquippable), nameof(ItemEquippable.RequestEquip))]
    public class EquipPatch
    {
        internal static List<CartItem> AllCartItems = [];
        public static void Postfix(ItemEquippable __instance)
        {
            if (!UpgradeManager.LocalItemsUpgrade && !HostValues.KeepNoUpgrade.Value)
                return;

            Plugin.Spam("Checking item being equipped");
            GetPocketCarts.AllSmallCarts.RemoveAll(c => c == null);
            if (!GetPocketCarts.AllSmallCarts.Any(e => e.itemEquippable == __instance))
                return;

            PhysGrabCart cart = GetPocketCarts.AllSmallCarts.FirstOrDefault(c => c.itemEquippable == __instance);
            if (cart == null)
                return;

            if (cart.itemsInCart.Count == 0)
                return;

            CartManager cartManager = cart.GetComponent<CartManager>();

            if(cartManager == null)
            {
                Plugin.ERROR("Unable to get cartManager component!");
                return;
            }

            if (cartManager.isShowingItems)
            {
                Plugin.WARNING("cart is currently showing items!");
                return;
            }

            if (!HostValues.KeepNoUpgrade.Value && HostValues.KeepItemsLevels.Value)
            {

                //compare local upgrade level to amount of carts storing items
                if (UpgradeManager.CartItemsUpgradeLevel <= CartManager.CartsStoringItems)
                {
                    Plugin.Message($"Unable to store items with this cart, already storing items in [ {CartManager.CartsStoringItems} ] carts!");
                    return;
                }      
            }

            if(Keyboard.current.altKey.isPressed && ModConfig.AllowDeposit.Value)
            {
                Plugin.Message($"Deposit key being pressed, not storing items in cart!");
                return;
            }
            
            if (SemiFunc.IsMultiplayer())
                cartManager.photonView.RPC("HideCartItems", RpcTarget.AllBuffered, PlayerAvatar.instance.steamID);
            else
                cartManager.HideCartItems(PlayerAvatar.instance.steamID);

            Plugin.Spam("Pocket cart equip detected!\nHiding all cart items with cart!");
        }

        internal static IEnumerator ChangeSize(float duration, Vector3 targetScale, Vector3 originalScale, Transform transform)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localScale = targetScale;
            Plugin.Spam($"All items size set to targetscale [ {targetScale} ] in transform!");
        }
    }

    [HarmonyPatch(typeof(ItemEquippable), nameof(ItemEquippable.UpdateVisuals))]
    public class UpdateVisualsPatch
    {
        public static void Postfix(ItemEquippable __instance)
        {
            if (__instance.currentState != ItemEquippable.ItemState.Unequipping)
                return;

            if (!UpgradeManager.LocalItemsUpgrade && !HostValues.KeepNoUpgrade.Value)
                return;

            Plugin.Spam("Unequip detected! Checking if this item is a cart we care about");
            GetPocketCarts.AllSmallCarts.RemoveAll(c => c == null);
            if (!GetPocketCarts.AllSmallCarts.Any(e => e.itemEquippable == __instance))
            {
                Plugin.Spam("We don't care about this equippable");
                return;
            }    

            PhysGrabCart cart = GetPocketCarts.AllSmallCarts.FirstOrDefault(c => c.itemEquippable == __instance);
            
            //we only care about one of our carts that has stored items
            if (cart == null)
            {
                Plugin.Spam("Cart is null");
                return;
            }

            CartManager cartManager = cart.GetComponent<CartManager>();

            if (cartManager == null)
            {
                Plugin.ERROR("Unable to get cartManager component!");
                return;
            }

            if (!cartManager.HasItems())
                return;

            if (SemiFunc.IsMultiplayer())
                cartManager.photonView.RPC("ShowCartItems", RpcTarget.AllBuffered);
            else
                cartManager.ShowCartItems();
        }

        internal static IEnumerator WaitToDisplay(CartManager cartManager)
        {
            if (cartManager.isShowingItems)
                yield break;

            cartManager.isShowingItems = true;

            PhysGrabCart cart = cartManager.MyCart;
            Plugin.Spam($"Waiting to Display object group for cart!");
            yield return null;

            //wait for resize to complete
            while (cart.transform.localScale != Vector3.one) 
            {
                yield return null;
                if (cartManager.isStoringItems)
                {
                    Plugin.WARNING("Ending display early! Cart is equiping!");
                    cartManager.isShowingItems = false;
                    yield break;
                }
                    
            }

            Plugin.Spam($"AllCartItems count - {EquipPatch.AllCartItems.Count}\nRemoving null items!");

            EquipPatch.AllCartItems.RemoveAll(c => c == null || c.grabObj == null);

            Plugin.Spam($"AllCartItems count - {EquipPatch.AllCartItems.Count}\nRestoring items in cart!");
            EquipPatch.AllCartItems.DoIf(x => x.IsStored && x.MyCart == cart, x =>
            {
                cart.StartCoroutine(x.RestoreItem(cart));
            });

            yield return null;
            cartManager.isShowingItems = false;
            if(cartManager.storedBy == PlayerAvatar.instance.steamID)
                CartManager.CartsStoringItems = Mathf.Clamp(CartManager.CartsStoringItems - 1, 0, 99);
        }
    }
}
