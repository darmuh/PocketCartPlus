using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ItemEquippable;

namespace PocketCartPlus
{
    [HarmonyPatch(typeof(PhysGrabCart), "Start")]
    public class GetPocketCarts
    {
        public static List<PhysGrabCart> AllSmallCarts = [];
        public static Dictionary<PhysGrabCart, Vector3> cartScale = [];
        public static void Postfix(PhysGrabCart __instance)
        {
            if (!__instance.isSmallCart)
                return;

            AllSmallCarts.RemoveAll(c => c == null);
            AllSmallCarts.Add(__instance);
            cartScale.Add(__instance, __instance.transform.localScale);

        }
    }

    //need to patch here to parent transform early enough
    [HarmonyPatch(typeof(ItemEquippable), "RequestEquip")]
    public class EquipPatch
    {
        internal static List<CartItem> AllCartItems = [];
        public static void Postfix(ItemEquippable __instance)
        {
            Plugin.Spam("Checking item being equipped");
            GetPocketCarts.AllSmallCarts.RemoveAll(c => c == null);
            if (!GetPocketCarts.AllSmallCarts.Any(e => e.itemEquippable == __instance))
                return;

            PhysGrabCart cart = GetPocketCarts.AllSmallCarts.FirstOrDefault(c => c.itemEquippable == __instance);
            if (cart == null)
                return;

            Plugin.Spam("Pocket cart equip detected!\nHiding all cart items with cart!");
            cart.itemsInCart.Do(c =>
            {
                AddToEquip(c, cart);
            });

            AllCartItems.RemoveAll(c => c.actualItem == null);
        }

        internal static void AddToEquip(PhysGrabObject item, PhysGrabCart cart)
        {
            item.rb.isKinematic = true;
            item.isActive = false;
            item.impactDetector.enabled = false;
            item.impactDetector.isIndestructible = true;
            CartItem cartItem;
            if (AllCartItems.Count == 0)
                cartItem = new(item, cart);
            else
            {
                cartItem = AllCartItems.FirstOrDefault(c => c.actualItem == item);
                if (cartItem == null)
                    cartItem = new(item, cart);
                else
                    cartItem.UpdateItem(item, cart);
            }

            List<Collider> coll = [.. item.gameObject.GetComponentsInChildren<Collider>()];
            coll.Do(c => c.enabled = false);
            cart.StartCoroutine(ChangeSize(0.2f, item.transform.localScale * 0.01f, item.transform.localScale, item.transform));

            cartItem.isStored = true;
            Plugin.Spam($"{item.gameObject.name} has been equipped with the cart!");
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
        //config?
        internal static WaitForSeconds invulnwait = new(0.6f);
        public static void Postfix(ItemEquippable __instance)
        {
            if (__instance.currentState != ItemState.Unequipping)
                return;

            Plugin.Spam("Unequip detected! Checking if this item is a cart we care about");
            GetPocketCarts.AllSmallCarts.RemoveAll(c => c == null);
            if (!GetPocketCarts.AllSmallCarts.Any(e => e.itemEquippable == __instance))
                return;

            PhysGrabCart cart = GetPocketCarts.AllSmallCarts.FirstOrDefault(c => c.itemEquippable == __instance);
            
            //we only care about one of our carts that has stored items
            if (cart == null || EquipPatch.AllCartItems.FindAll(x => x.isStored).Count == 0)
                return;

            Plugin.Spam("Pocket cart with items detected!");
            cart.StartCoroutine(WaitToDisplay(cart));        
        }

        internal static IEnumerator WaitToDisplay(PhysGrabCart cart)
        {
            Plugin.Spam($"Waiting to Display object group");
            yield return null;

            //wait for resize to complete
            while (cart.transform.localScale != Vector3.one) 
            {
                yield return null;
            }

            Plugin.Spam("small cart has spawned!");
            Plugin.Spam($"cart position: {cart.inCart.position}");

            Plugin.Spam($"cart position: {cart.inCart.position}");

            Vector3 cartOriginal = Vector3.one;
            if (GetPocketCarts.cartScale.TryGetValue(cart, out cartOriginal))
                Plugin.Spam($"cart original scale is {cartOriginal}");

            EquipPatch.AllCartItems.RemoveAll(c => c.actualItem == null);

            Plugin.Spam("Restoring items in cart");
            EquipPatch.AllCartItems.DoIf(x => x.isStored, x =>
            {
                x.actualItem.StartCoroutine(RestoreItem(x, cart, cartOriginal));
            });
        }

        private static IEnumerator RestoreItem(CartItem cartItem, PhysGrabCart cart, Vector3 cartOriginal)
        {
            Plugin.Spam($"{cartItem.actualItem.gameObject.name} position: {cartItem.actualItem.gameObject.transform.position}");

            cartItem.isStored = false;
            cartItem.actualItem.isActive = true;
            cartItem.actualItem.transform.localScale = cart.transform.localScale;
            cartItem.actualItem.transform.position = ClampToCartBounds(cart, cartItem);

            //wait for cart to stabilize, config item for timer?
            while (PlayerAvatar.instance.physGrabber.overrideGrab && cart.draggedTimer < 0.75f)
            {
                float t = cart.draggedTimer / 0.75f;
                if(cartOriginal != cartItem.actualItem.transform.localScale)
                    cartItem.actualItem.transform.localScale = Vector3.Lerp(cartItem.actualItem.transform.localScale, Vector3.one, t);
                cartItem.actualItem.transform.position = ClampToCartBounds(cart, cartItem);
                yield return null;
            }

            List<Collider> coll = [.. cartItem.actualItem.gameObject.GetComponentsInChildren<Collider>()];
            coll.Do(c => c.enabled = true);
            cartItem.actualItem.transform.localScale = Vector3.one;
            cartItem.actualItem.impactDetector.enabled = true;
            yield return invulnwait;
            cartItem.actualItem.rb.isKinematic = false;
            cartItem.actualItem.impactDetector.isIndestructible = false;
            
        }

        private static Vector3 ClampToCartBounds(PhysGrabCart cart, CartItem item)
        {
            Vector3 newPosition = cart.inCart.position + new Vector3(
                item.PosOffset.x * cart.inCart.localScale.x,
                item.PosOffset.y * cart.inCart.localScale.y,
                item.PosOffset.z * cart.inCart.localScale.z
            );
            Bounds combinedBounds = cart.capsuleColliders[0].bounds;

            combinedBounds.min += new Vector3(0.2f, 0.2f, 0.2f);
            combinedBounds.max -= new Vector3(0.2f, -1f, 0.2f);

            cart.capsuleColliders.Do(c => combinedBounds.Encapsulate(c.bounds));

            return item.actualItem.transform.position = new Vector3(
                Mathf.Clamp(newPosition.x, combinedBounds.min.x, combinedBounds.max.x),
                Mathf.Clamp(newPosition.y, combinedBounds.min.y, combinedBounds.max.y),
                Mathf.Clamp(newPosition.z, combinedBounds.min.z, combinedBounds.max.z)
            );
        }
    }
}
