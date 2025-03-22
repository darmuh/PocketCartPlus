using ExitGames.Client.Photon;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using REPOLib.Modules;
using static ItemEquippable;

namespace PocketCartPlus
{

    [HarmonyPatch(typeof(PlayerAvatar), "Spawn")]
    public class SpawnPlayerPatch
    {
        public static void Postfix()
        {
            if (!AreWeInGame())
                return;

            if (SemiFunc.IsMasterClientOrSingleplayer())
            {
                PocketCartUpgradeItems.HostStart();
            }
            else
            {
                PocketCartUpgradeItems.ClientStart();
            }
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

            PocketCartUpgradeItems.ValueRef();

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
            keepItems.value = PocketCartUpgradeItems.valuePreset;
            Plugin.Spam($"Value preset set for KeepPocketCartItems Upgrade!");
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

            AllSmallCarts.RemoveAll(c => c == null);
            AllSmallCarts.Add(__instance);
        }
    }

    //need to patch here to parent transform early enough
    [HarmonyPatch(typeof(ItemEquippable), "RequestEquip")]
    public class EquipPatch
    {
        internal static List<CartItem> AllCartItems = [];
        public static void Postfix(ItemEquippable __instance)
        {
            if (!PocketCartUpgradeItems.localItemsUpgrade)
                return;

            Plugin.Spam("Checking item being equipped");
            GetPocketCarts.AllSmallCarts.RemoveAll(c => c == null);
            if (!GetPocketCarts.AllSmallCarts.Any(e => e.itemEquippable == __instance))
                return;

            PhysGrabCart cart = GetPocketCarts.AllSmallCarts.FirstOrDefault(c => c.itemEquippable == __instance);
            if (cart == null)
                return;

            Plugin.Spam("Pocket cart equip detected!\nHiding all cart items with cart!");
            Networking.HideCartItems.RaiseEvent(cart.photonView.InstantiationId, NetworkingEvents.RaiseAll, SendOptions.SendReliable);
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

            if (!PocketCartUpgradeItems.localItemsUpgrade)
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
            if (cart == null || EquipPatch.AllCartItems.FindAll(x => x.isStored).Count == 0)
            {
                Plugin.Spam("Cart is null or has no items");
                return;
            }

            Networking.ShowCartItems.RaiseEvent(cart.photonView.InstantiationId, NetworkingEvents.RaiseAll, SendOptions.SendReliable);
        }

        internal static IEnumerator WaitToDisplay(PhysGrabCart cart)
        {
            Plugin.Spam($"Waiting to Display object group for cart!");
            yield return null;

            //wait for resize to complete
            while (cart.transform.localScale != Vector3.one) 
            {
                yield return null;
            }

            //Plugin.Spam("small cart has spawned!");
            //Plugin.Spam($"cart position: {cart.inCart.position}");

            //Plugin.Spam($"cart position: {cart.inCart.position}");

            Plugin.Spam($"AllCartItems count - {EquipPatch.AllCartItems.Count}\nRemoving null items!");

            EquipPatch.AllCartItems.RemoveAll(c => c == null || c.actualItem == null);

            Plugin.Spam($"AllCartItems count - {EquipPatch.AllCartItems.Count}\nRestoring items in cart!");
            EquipPatch.AllCartItems.DoIf(x => x.isStored && x.MyCart == cart, x =>
            {
                cart.StartCoroutine(x.RestoreItem(cart));
            });
        }
    }
}
