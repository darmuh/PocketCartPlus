using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ItemEquippable;

namespace PocketCartPlus
{

    [HarmonyPatch(typeof(Map), "CustomPositionSet")]
    public class MapFixPatch
    {
        public static void Postfix(Transform transformTarget, Transform transformSource)
        {
            if (SemiFunc.RunIsShop())
                return;

            //target is the mapicon, source is the actual object
            CartItem cartItem = transformSource.gameObject.GetComponent<CartItem>() ?? transformSource.gameObject.AddComponent<CartItem>();
            
            if (cartItem.mapIcon != null)
                return;

            Plugin.Spam($"Assigning mapIcon for game object {transformSource.gameObject.name}");
            cartItem.mapIcon = transformTarget.gameObject.GetComponentInChildren<SpriteRenderer>();

        }

        private static bool AreWeInGame()
        {
            if (SemiFunc.RunIsLobbyMenu())
                return false;

            if (RunManager.instance.levelCurrent == RunManager.instance.levelMainMenu)
                return false;

            return true;
        }
    }

    [HarmonyPatch(typeof(StatsManager), "Awake")]
    public class StatsManagerAwake
    {
        public static void Postfix()
        {
            PocketCartUpgradeItems.InitDictionary();
        }
    }

    [HarmonyPatch(typeof(StatsManager), "Start")]
    public class StatsManagerStart
    {
        public static void Postfix(StatsManager __instance)
        {
            Plugin.Spam("Updating statsmanager with our save keys!");
            if (!__instance.dictionaryOfDictionaries.ContainsKey("playerUpgradePocketcartKeepItems"))
                __instance.dictionaryOfDictionaries.Add("playerUpgradePocketcartKeepItems", PocketCartUpgradeItems.dictionaryOfClients);
        }
    }

    [HarmonyPatch(typeof(RunManager), "ResetProgress")]
    public class ResetStuff
    {
        public static void Postfix()
        {
            PocketCartUpgradeItems.ResetProgress();
        }
    }

    //price and rarity config patch
    [HarmonyPatch(typeof(SemiFunc), "ShopPopulateItemVolumes")]
    public class ModifyItemRarity
    {
        public static void Prefix()
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;

            //prices
            PocketCartUpgradeItems.ValueRef();
            PocketCartUpgradeSize.ValueRef();

            //add-on rarities
            PocketCartUpgradeItems.ShopPatch();
            PocketCartUpgradeSize.ShopPatch();
        }
    }

    [HarmonyPatch(typeof(PhysGrabCart), "Start")]
    public class GetPocketCarts
    {
        public static List<PhysGrabCart> AllSmallCarts = [];
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
    [HarmonyPatch(typeof(ItemEquippable), "AnimateUnequip")]
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
    [HarmonyPatch(typeof(ItemEquippable), "RequestEquip")]
    public class EquipPatch
    {
        internal static List<CartItem> AllCartItems = [];
        public static void Postfix(ItemEquippable __instance)
        {
            if (!UpgradeManager.LocalItemsUpgrade && !ModConfig.KeepItemsUnlockNoUpgrade.Value)
                return;

            Plugin.Spam("Checking item being equipped");
            GetPocketCarts.AllSmallCarts.RemoveAll(c => c == null);
            if (!GetPocketCarts.AllSmallCarts.Any(e => e.itemEquippable == __instance))
                return;

            PhysGrabCart cart = GetPocketCarts.AllSmallCarts.FirstOrDefault(c => c.itemEquippable == __instance);
            if (cart == null)
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

            if (!ModConfig.KeepItemsUnlockNoUpgrade.Value && ModConfig.CartItemLevels.Value)
            {

                //compare local upgrade level to amount of carts storing items
                if (UpgradeManager.CartItemsUpgradeLevel <= CartManager.CartsStoringItems)
                {
                    Plugin.Spam($"Unable to store items with this cart, already storing items in [ {CartManager.CartsStoringItems} ] carts!");
                    return;
                }
                else
                    CartManager.CartsStoringItems++;
            }
            
            if (SemiFunc.IsMultiplayer())
                cartManager.photonView.RPC("HideCartItems", Photon.Pun.RpcTarget.All);
            else
                cartManager.HideCartItems();

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

    [HarmonyPatch(typeof(ItemEquippable), "UpdateVisuals")]
    public class UpdateVisualsPatch
    {
        public static void Postfix(ItemEquippable __instance)
        {
            if (__instance.currentState != ItemState.Unequipping)
                return;

            if (!UpgradeManager.LocalItemsUpgrade)
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

            if (!cartManager.hasItems)
                return;

            if (SemiFunc.IsMultiplayer())
                cartManager.photonView.RPC("ShowCartItems", Photon.Pun.RpcTarget.All);
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
            EquipPatch.AllCartItems.DoIf(x => x.isStored && x.MyCart == cart, x =>
            {
                cart.StartCoroutine(x.RestoreItem(cart));
            });

            yield return null;
            cartManager.isShowingItems = false;
            cartManager.hasItems = false;
            CartManager.CartsStoringItems--;
        }
    }
}
