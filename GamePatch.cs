using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ItemEquippable;

namespace PocketCartPlus
{
    [HarmonyPatch(typeof(EventData))]
    [HarmonyPatch(MethodType.Constructor)]
    public class Networking
    {
        public static List<NetworkedEvent> AllCustomEvents = [];
        public static List<EventData> AllEvents = [];
        internal static NetworkedEvent HideCartItems;
        internal static NetworkedEvent ShowCartItems;
        internal static Dictionary<int, Action> NetworkEvents;
        public static void Postfix(EventData __instance)
        {
            if(AllCustomEvents.Any(b => b.EventByte == __instance.Code))
            {
                Plugin.WARNING("New EventData appears to be using the same code as a custom event!");
                //above may be shown even for our actual custom events
            }
            AllEvents.Add(__instance);
        }

        public static RaiseEventOptions RaiseAll = new()
        {
            Receivers = ReceiverGroup.All
        };

        public static RaiseEventOptions RaiseOthers = new()
        {
            Receivers = ReceiverGroup.Others
        };

        internal static void Init()
        {
            HideCartItems ??= new("HideCartItems", CartItem.HideCartItems);
            ShowCartItems ??= new("ShowCartItems", CartItem.ShowCartItems);

            PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
        }

        private static void OnEvent(EventData photonEvent)
        {
            Plugin.Spam("Photon Event detected!");

            NetworkedEvent thisEvent = AllCustomEvents.FirstOrDefault(e => e.EventByte == photonEvent.Code);
            if (thisEvent == null)
                return;

            thisEvent.EventAction.Invoke(photonEvent.CustomData);
        }
    }

    [HarmonyPatch(typeof(RunManager), "Awake")]
    [HarmonyPriority(Priority.Last)]
    public class InitThings
    {
        public static void Postfix()
        {
            Networking.Init();
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
            Plugin.Spam("Checking item being equipped");
            GetPocketCarts.AllSmallCarts.RemoveAll(c => c == null);
            if (!GetPocketCarts.AllSmallCarts.Any(e => e.itemEquippable == __instance))
                return;

            PhysGrabCart cart = GetPocketCarts.AllSmallCarts.FirstOrDefault(c => c.itemEquippable == __instance);
            if (cart == null)
                return;

            Plugin.Spam("Pocket cart equip detected!\nHiding all cart items with cart!");
            if (SemiFunc.IsMultiplayer())
                PhotonNetwork.RaiseEvent(Networking.HideCartItems.EventByte, GetPocketCarts.AllSmallCarts.IndexOf(cart), Networking.RaiseOthers, SendOptions.SendReliable);

            Networking.HideCartItems.EventAction.Invoke(GetPocketCarts.AllSmallCarts.IndexOf(cart));
        }

        internal static void AddToEquip(PhysGrabObject item, PhysGrabCart cart)
        {
            //Disable item physics
            item.rb.isKinematic = true;
            item.isActive = false;
            item.impactDetector.enabled = false;
            item.impactDetector.isIndestructible = true;


            //Create/Update CartItem
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

            //Collider Disable
            List<Collider> coll = [.. item.gameObject.GetComponentsInChildren<Collider>()];
            coll.Do(c => c.enabled = false);
            cart.StartCoroutine(ChangeSize(0.2f, item.transform.localScale * 0.01f, item.transform.localScale, item.transform));

            //Enemy Disable
            if (cartItem.EnemyBody != null)
            {
                cartItem.EnemyBody.enabled = false;

                if (cartItem.EnemyBody.enemy.HasNavMeshAgent)
                    cartItem.EnemyBody.enemy.NavMeshAgent.enabled = false;

                cartItem.EnemyBody.enemyParent.StartCoroutine(ChangeSize(0.2f, cartItem.EnemyBody.enemyParent.transform.localScale * 0.01f, cartItem.EnemyBody.enemyParent.transform.localScale, cartItem.EnemyBody.enemyParent.transform));
            }

            //Lights Disable
            if (cartItem.itemLights.Count > 0)
                cartItem.itemLights.Do(i => i.enabled = false);


            //Set stored
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
            if (SemiFunc.IsMultiplayer())
                PhotonNetwork.RaiseEvent(Networking.ShowCartItems.EventByte, GetPocketCarts.AllSmallCarts.IndexOf(cart), Networking.RaiseOthers, SendOptions.SendReliable);

            Networking.ShowCartItems.EventAction.Invoke(GetPocketCarts.AllSmallCarts.IndexOf(cart));
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

            EquipPatch.AllCartItems.RemoveAll(c => c.actualItem == null);

            Plugin.Spam("Restoring items in cart");
            EquipPatch.AllCartItems.DoIf(x => x.isStored, x =>
            {
                x.actualItem.StartCoroutine(RestoreItem(x, cart));
            });
        }

        private static void LerpItem(CartItem cartItem, PhysGrabCart cart, float t)
        {
            if (cartItem.OriginalScale != cartItem.actualItem.transform.localScale)
                cartItem.actualItem.transform.localScale = Vector3.Lerp(cartItem.actualItem.transform.localScale, cartItem.OriginalScale, t);

            if (cartItem.EnemyBody != null)
            {
                if (cartItem.OriginalEnemyScale != cartItem.EnemyBody.enemyParent.transform.localScale)
                    cartItem.EnemyBody.enemyParent.transform.localScale = Vector3.Lerp(cartItem.EnemyBody.enemyParent.transform.localScale, cartItem.OriginalEnemyScale, t);
            }

            cartItem.actualItem.transform.position = ClampToCartBounds(cart, cartItem);
        }

        private static IEnumerator RestoreItem(CartItem cartItem, PhysGrabCart cart)
        {
            Plugin.Spam($"{cartItem.actualItem.gameObject.name} position: {cartItem.actualItem.gameObject.transform.position}");

            //Enable item physics
            cartItem.isStored = false;
            cartItem.actualItem.isActive = true;
            cartItem.actualItem.transform.localScale = cart.transform.localScale;
            cartItem.actualItem.transform.position = ClampToCartBounds(cart, cartItem);

            //enable item lights
            if (cartItem.itemLights.Count > 0)
                cartItem.itemLights.Do(i => i.enabled = true);

            if (PlayerAvatar.instance.deadSet)
            {
                //player is dead, no need to worry about cart stabilizing
                float deadTimer = 0.25f;
                while (deadTimer > 0f)
                {
                    deadTimer -= Time.deltaTime;
                    LerpItem(cartItem, cart, deadTimer);
                    yield return null;
                }
            }
            else
            {
                //wait for cart to stabilize, config item for timer?
                while (PlayerAvatar.instance.physGrabber.overrideGrab && cart.draggedTimer < 0.25f)
                {
                    float t = cart.draggedTimer / 0.25f; //not sure why I am caluclating it like this
                    LerpItem(cartItem, cart, t);
                    yield return null;
                }
            }
                
            //Enable colliders
            List<Collider> coll = [.. cartItem.actualItem.gameObject.GetComponentsInChildren<Collider>()];
            coll.Do(c => c.enabled = true);

            //Enable Enemy stuff
            if (cartItem.EnemyBody != null)
            {
                cartItem.EnemyBody.enabled = true;
                if (cartItem.EnemyBody.enemy.HasNavMeshAgent)
                {
                    Plugin.Spam("Enabling navmesh");
                    cartItem.EnemyBody.enemy.NavMeshAgent.enabled = true;
                    cartItem.EnemyBody.enemy.NavMeshAgent.ResetPath();
                }
                yield return null;
                Plugin.Spam("Returning enemy state");
                cartItem.EnemyBody.enemy.CurrentState = cartItem.OriginalState;
            }

            //Set to original scale and enable destructability
            cartItem.actualItem.transform.localScale = cartItem.OriginalScale;
            cartItem.actualItem.impactDetector.enabled = true;
            yield return invulnwait; // configurable invulnerability
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
