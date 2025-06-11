using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PocketCartPlus
{

    //for adding UI hint to deposit items
    [HarmonyPatch(typeof(PhysGrabCart), "StateMessages")]
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

            if (!ModConfig.KeepItemsUnlockNoUpgrade.Value && !UpgradeManager.LocalItemsUpgrade)
                return;

            if (!ModConfig.KeepItemsUnlockNoUpgrade.Value && ModConfig.CartItemLevels.Value && UpgradeManager.CartItemsUpgradeLevel <= CartManager.CartsStoringItems)
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

    [HarmonyPatch(typeof(ItemInfoExtraUI), "Start")]
    public class CreateHintUI
    {
        public static GameObject Hinter;
        public static void Postfix(ItemInfoExtraUI __instance)
        {
            if (HintUI.instance != null || __instance.Text == null)
                return;

            Plugin.Spam("Creating Hinter!");
            Hinter = new("Hinter");
            //GameObject.Instantiate<TextMeshProUGUI>(ItemInfoExtraUI.instance.Text, Hinter.gameObject.transform);
            TextMeshProUGUI text = Hinter.AddComponent<TextMeshProUGUI>();
            text.font = __instance.Text.font;
            text.fontMaterial = __instance.Text.fontMaterial;
            text.fontSharedMaterial = __instance.Text.fontSharedMaterial;
            text.fontSharedMaterials = __instance.Text.fontSharedMaterials;
            text.spriteAsset = __instance.Text.spriteAsset;
            text.alignment = TextAlignmentOptions.Midline;
            text.fontSize = 12f;
            text.fontStyle = FontStyles.SmallCaps;
            Hinter.AddComponent<RectTransform>();
            Hinter.transform.SetParent(__instance.gameObject.transform);
            Hinter.AddComponent<HintUI>();
        }
    }

    [HarmonyPatch(typeof(SemiFunc), "OnLevelGenDone")]
    public class PocketDimension
    {
        internal static GameObject ThePocket;
        internal static VoidRef voidRef;
        public static void Postfix()
        {
            if (!SpawnPlayerStuff.AreWeInGame())
                return;

            ThePocket = GameObject.Instantiate(Plugin.PocketDimension, new Vector3(999f, 0, 0), new Quaternion(), LevelGenerator.Instance.LevelParent.transform);
            voidRef = ThePocket.GetComponentInChildren<VoidRef>();
            Plugin.Spam($"voidRef is null - {voidRef == null}");
            //TeleportMe();

        }

        internal static void TeleportPlayer(PlayerAvatar player, Vector3 position, Quaternion rotation)
        {
            if (player.isLocal)
            {
                if(player.isTumbling)
                    player.tumble.TumbleSet(false, false);

                PlayerController.instance.transform.position = position;
                PlayerController.instance.transform.rotation = rotation;
            }

            player.rb.position = position;
            player.rb.rotation = rotation;
            player.transform.position = position;
            player.transform.rotation = rotation;
            player.clientPosition = position;
            player.clientPositionCurrent = position;
            player.clientRotation = rotation;
            player.clientRotationCurrent = rotation;
            player.playerAvatarVisuals.visualPosition = position;

            Plugin.Spam($"{player.playerName} teleported to position {position}");
        }
    }

    [HarmonyPatch(typeof(CameraAim), "CameraAimSpawn")]
    public class SpawnPlayerStuff
    {
        public static void Postfix()
        {
            if (!AreWeInGame())
                return;

            //Reset CartsStoringItems amount (since they have just spawned and cant possibly be storing items)
            Plugin.Message("Player Spawned: CartsStoringItems set to 0");
            CartManager.CartsStoringItems = 0;

            if(PocketCartUpgradeItems.LoadNewSave)
                PocketCartUpgradeItems.ClientsUnlocked();
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

    [HarmonyPatch(typeof(StatsManager), "LoadGame")]
    public class StatsManagerLoad
    {
        public static void Postfix()
        {
            PocketCartUpgradeItems.LoadStart();
        }
    }

    [HarmonyPatch(typeof(StatsManager), "Start")]
    public class StatsManagerStart
    {
        public static void Postfix(StatsManager __instance)
        {
            PocketCartUpgradeItems.InitDictionary();
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

    //LeaveToMainMenu
    [HarmonyPatch(typeof(RunManager), "LeaveToMainMenu")]
    public class LeaveToMainReset
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

            if (!ModConfig.KeepItemsUnlockNoUpgrade.Value && ModConfig.CartItemLevels.Value)
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
                cartManager.photonView.RPC("HideCartItems", Photon.Pun.RpcTarget.All, PlayerAvatar.instance.steamID);
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

    [HarmonyPatch(typeof(ItemEquippable), "UpdateVisuals")]
    public class UpdateVisualsPatch
    {
        public static void Postfix(ItemEquippable __instance)
        {
            if (__instance.currentState != ItemEquippable.ItemState.Unequipping)
                return;

            if (!UpgradeManager.LocalItemsUpgrade && !ModConfig.KeepItemsUnlockNoUpgrade.Value)
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
            if(cartManager.storedBy == PlayerAvatar.instance.steamID)
                CartManager.CartsStoringItems = Mathf.Clamp(CartManager.CartsStoringItems - 1, 0, 99);
        }
    }
}
