using ExitGames.Client.Photon;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PocketCartPlus
{
    internal class CartItem : MonoBehaviour
    {
        internal Vector3 PosOffset;
        //internal Quaternion QuartOffset;
        internal PhysGrabObject actualItem;
        internal bool isStored = false;
        internal Vector3 OriginalScale;
        internal Vector3 OriginalEnemyScale;

        internal EnemyRigidbody EnemyBody = null!;
        internal EnemyState OriginalState = EnemyState.None;
        internal List<Light> itemLights = [];

        internal PhysGrabCart MyCart;

        internal void UpdateItem(PhysGrabObject item, PhysGrabCart cart)
        {
            if (item == null || cart == null)
            {
                Plugin.ERROR("Unable to update CartItem off of null cart or item!");
                return;
            }

            PosOffset = item.transform.position - cart.inCart.position;
            OriginalScale = item.transform.localScale;

            MyCart = cart;
            actualItem = item;

            itemLights = [.. gameObject.GetComponentsInChildren<Light>()];

            if (item.isEnemy)
            {
                EnemyBody = gameObject.GetComponent<EnemyRigidbody>();
                OriginalEnemyScale = EnemyBody.enemyParent.transform.localScale;
                OriginalState = EnemyBody.enemy.CurrentState;

                List<Light> enemyLights = [.. EnemyBody.enemyParent.gameObject.GetComponentsInChildren<Light>()];

                if (enemyLights.Count > 0)
                    itemLights.AddRange(enemyLights);
            }

            itemLights.Distinct();
            itemLights.RemoveAll(i => i.enabled = false);

            //calculate scale
            PosOffset = new Vector3(
                PosOffset.x / cart.inCart.localScale.x,
                PosOffset.y / cart.inCart.localScale.y,
                PosOffset.z / cart.inCart.localScale.z
                );
            //QuartOffset = Quaternion.Inverse(cart.transform.rotation) * item.transform.rotation;
            Plugin.Spam($"---------------\nCartItem updated to store {item.gameObject.name}'s offsets:\nPosOffset - {PosOffset}\n---------------");

            if(!EquipPatch.AllCartItems.Contains(this))
                EquipPatch.AllCartItems.Add(this);
        }

        internal static void HideCartItems(EventData eventData)
        {
            Plugin.Spam("HideCartItems detected!");
            int cartIndex = (int)eventData.CustomData;

            GetPocketCarts.AllSmallCarts.RemoveAll(c => c == null);
            PhysGrabCart cart = GetPocketCarts.AllSmallCarts.Find(c => c.photonView.InstantiationId == cartIndex);

            if (cart == null)
            {
                Plugin.WARNING($"cartIndex is invalid! [ {cartIndex} ]");
                return;
            }
                
            cart.itemsInCart.Do(c =>
            {
                Plugin.Spam($"AddToEquip [ {c.gameObject.name} ]");
                AddToEquip(c, cart);
            });

            EquipPatch.AllCartItems.RemoveAll(c => c.actualItem == null);
        }

        private static void AddToEquip(PhysGrabObject item, PhysGrabCart cart)
        {
            if (item.isPlayer && ModConfig.PlayerSafetyCheck.Value)
            {
                Plugin.Spam("Skipping player!");
                return;
            }
            
            if (item.isEnemy && ModConfig.IgnoreEnemies.Value)
            {
                Plugin.Spam("Ignoring Enemy!");
                return;
            }    

            //Disable item physics
            item.rb.isKinematic = true;
            item.isActive = false;
            item.impactDetector.enabled = false;
            item.impactDetector.isIndestructible = true;
            
            //Create/Update CartItem
            CartItem cartItem = item.gameObject.GetComponent<CartItem>() ?? item.gameObject.AddComponent<CartItem>();
            cartItem.UpdateItem(item, cart);

            Plugin.Spam($"cartItem.actualItem is null [ {cartItem.actualItem == null} ]");

            //Collider Disable
            List<Collider> coll = [.. item.gameObject.GetComponentsInChildren<Collider>()];
            coll.Do(c => c.enabled = false);
            cart.StartCoroutine(EquipPatch.ChangeSize(0.2f, item.transform.localScale * 0.01f, item.transform.localScale, item.transform));

            //Enemy Disable
            if (cartItem.EnemyBody != null)
            {
                cartItem.EnemyBody.enabled = false;

                if (cartItem.EnemyBody.enemy.HasNavMeshAgent)
                    cartItem.EnemyBody.enemy.NavMeshAgent.enabled = false;

                cartItem.EnemyBody.enemyParent.StartCoroutine(EquipPatch.ChangeSize(0.2f, cartItem.EnemyBody.enemyParent.transform.localScale * 0.01f, cartItem.EnemyBody.enemyParent.transform.localScale, cartItem.EnemyBody.enemyParent.transform));
            }

            //Lights Disable
            if (cartItem.itemLights.Count > 0)
                cartItem.itemLights.Do(i => i.enabled = false);

            //Set stored
            cartItem.isStored = true;
            Plugin.Spam($"{item.gameObject.name} has been equipped with the cart!");
            Plugin.Spam($"cartItem.actualItem is null [ {cartItem.actualItem == null} ]");
        }

        internal static void ShowCartItems(EventData eventData)
        {
            Plugin.Message("NETWORK EVENT: ShowCartItems detected!");
            int cartIndex = (int)eventData.CustomData;

            GetPocketCarts.AllSmallCarts.RemoveAll(c => c == null);
            PhysGrabCart cart = GetPocketCarts.AllSmallCarts.Find(c => c.photonView.InstantiationId == cartIndex);

            if (cart == null)
            {
                Plugin.WARNING($"cartIndex is invalid! [ {cartIndex} ]");
                return;
            }

            Plugin.Spam("Starting cart coroutine!");
            cart.StartCoroutine(UpdateVisualsPatch.WaitToDisplay(cart));
        }

        internal Vector3 ClampToCartBounds(PhysGrabCart cart)
        {
            Vector3 newPosition = cart.inCart.position + new Vector3(
                PosOffset.x * cart.inCart.localScale.x,
                PosOffset.y * cart.inCart.localScale.y,
                PosOffset.z * cart.inCart.localScale.z
            );
            Bounds combinedBounds = cart.capsuleColliders[0].bounds;

            combinedBounds.min += new Vector3(0.2f, 0.2f, 0.2f);
            combinedBounds.max -= new Vector3(0.2f, -1f, 0.2f);

            cart.capsuleColliders.Do(c => combinedBounds.Encapsulate(c.bounds));

            return actualItem.transform.position = new Vector3(
                Mathf.Clamp(newPosition.x, combinedBounds.min.x, combinedBounds.max.x),
                Mathf.Clamp(newPosition.y, combinedBounds.min.y, combinedBounds.max.y),
                Mathf.Clamp(newPosition.z, combinedBounds.min.z, combinedBounds.max.z)
            );
        }

        private void LerpItem(PhysGrabCart cart, float t)
        {
            if (OriginalScale != actualItem.transform.localScale)
                transform.localScale = Vector3.Lerp(actualItem.transform.localScale, OriginalScale, t);

            if (EnemyBody != null)
            {
                if (OriginalEnemyScale != EnemyBody.enemyParent.transform.localScale)
                    EnemyBody.enemyParent.transform.localScale = Vector3.Lerp(EnemyBody.enemyParent.transform.localScale, OriginalEnemyScale, t);
            }

            actualItem.transform.position = ClampToCartBounds(cart);
        }

        internal IEnumerator RestoreItem(PhysGrabCart cart)
        {
            Plugin.Spam($"{gameObject.name} position: {gameObject.transform.position}");
            Plugin.Spam($"actualItem is null [ {actualItem == null} ]");

            //Enable item physics
            isStored = false;
            actualItem.isActive = true;
            actualItem.transform.localScale = transform.localScale;
            transform.position = ClampToCartBounds(cart);

            //enable item lights
            if (itemLights.Count > 0)
                itemLights.Do(i => i.enabled = true);

            if (PlayerAvatar.instance.deadSet)
            {
                //player is dead, no need to worry about cart stabilizing
                float deadTimer = 0.1f;
                while (deadTimer > 0f)
                {
                    deadTimer -= Time.deltaTime;
                    LerpItem(cart, deadTimer);
                    yield return null;
                }
            }
            else
            {
                //wait for cart to stabilize
                while (PlayerAvatar.instance.physGrabber.overrideGrab && cart.draggedTimer < ModConfig.CartStabilizationTimer.Value)
                {
                    float t = cart.draggedTimer;
                    LerpItem(cart, t);
                    yield return null;
                }
            }

            //Enable colliders
            List<Collider> coll = [.. actualItem.gameObject.GetComponentsInChildren<Collider>()];
            coll.Do(c => c.enabled = true);

            //Enable Enemy stuff
            if (EnemyBody != null)
            {
                EnemyBody.enabled = true;
                if (EnemyBody.enemy.HasNavMeshAgent)
                {
                    Plugin.Spam("Enabling navmesh");
                    EnemyBody.enemy.NavMeshAgent.enabled = true;
                    EnemyBody.enemy.NavMeshAgent.ResetPath();
                }
                yield return null;
                Plugin.Spam("Returning enemy state");
                EnemyBody.enemy.CurrentState = OriginalState;
            }

            //Set to original scale and enable destructability
            actualItem.transform.localScale = OriginalScale;
            actualItem.impactDetector.enabled = true;
            actualItem.rb.isKinematic = false;
            yield return new WaitForSeconds(ModConfig.ItemSafetyTimer.Value);
            actualItem.impactDetector.isIndestructible = false;
            Plugin.Spam("RestoreItem complete!");
        }

    }
}
