using HarmonyLib;
using REPOLib.Modules;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PocketCartPlus
{
    internal class CartItem : MonoBehaviour
    {
        internal Vector3 PosOffset;
        internal PhysGrabObject grabObj;
        internal bool isStored = false;
        internal Transform baseTransform;
        internal Vector3 OriginalScale;
        //internal Vector3 OriginalEnemyScale;

        internal EnemyRigidbody EnemyBody = null!;
        internal EnemyState OriginalState = EnemyState.None;
        internal List<Light> itemLights = [];

        internal PhysGrabCart MyCart = null!;
        internal SpriteRenderer mapIcon = null!;
        internal bool hasMapIcon = false;

        internal void UpdateItem(PhysGrabObject item, PhysGrabCart cart)
        {
            if (item == null || cart == null)
            {
                Plugin.ERROR("Unable to update CartItem off of null cart or item!");
                return;
            }

            baseTransform = GetTransform(item);
            PosOffset = baseTransform.position - cart.inCart.position;
            OriginalScale = baseTransform.localScale;

            MyCart = cart;
            grabObj = item;
            hasMapIcon = mapIcon != null;

            itemLights = [.. gameObject.GetComponentsInChildren<Light>()];

            if (item.isEnemy)
            {
                EnemyBody = gameObject.GetComponent<EnemyRigidbody>();
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

        private Transform GetTransform(PhysGrabObject item)
        {
            if (item.isEnemy)
                return item.gameObject.gameObject.GetComponent<EnemyRigidbody>().enemyParent.gameObject.transform;

            if (item.isPlayer)
                return item.gameObject.GetComponent<PlayerAvatar>().gameObject.transform;

            return item.gameObject.transform;
        }

        internal static void AddToEquip(PhysGrabObject item, PhysGrabCart cart)
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

            //Collider Disable
            List<Collider> coll = [.. item.gameObject.GetComponentsInChildren<Collider>()];
            coll.Do(c => c.enabled = false);
            

            //Enemy Disable
            if (cartItem.EnemyBody != null)
            {
                cartItem.EnemyBody.enabled = false;
                if (cartItem.EnemyBody.enemy.HasNavMeshAgent && SemiFunc.IsMasterClientOrSingleplayer())
                    cartItem.EnemyBody.enemy.NavMeshAgent.Disable(9999999f);

                //cartItem.EnemyBody.enemyParent.StartCoroutine(EquipPatch.ChangeSize(0.2f, cartItem.EnemyBody.enemyParent.transform.localScale * 0.01f, cartItem.EnemyBody.enemyParent.transform.localScale, cartItem.EnemyBody.enemyParent.transform));
            }

            cart.StartCoroutine(EquipPatch.ChangeSize(0.2f, cartItem.baseTransform.localScale * 0.01f, cartItem.baseTransform.localScale, cartItem.baseTransform));

            //Lights Disable
            if (cartItem.itemLights.Count > 0)
                cartItem.itemLights.Do(i => i.enabled = false);

            //Set stored
            cartItem.isStored = true;

            if (cartItem.hasMapIcon)
            {
                if (cartItem.mapIcon != null)
                    cartItem.mapIcon.enabled = false;
                else
                    Plugin.WARNING($"Unable to hide map icon for {item.gameObject.name}");
            }
            
            Plugin.Spam($"{item.gameObject.name} has been equipped with the cart!");
            Plugin.Spam($"cartItem.grabObj is null [ {cartItem.grabObj == null} ]");
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

            return baseTransform.position = new Vector3(
                Mathf.Clamp(newPosition.x, combinedBounds.min.x, combinedBounds.max.x),
                Mathf.Clamp(newPosition.y, combinedBounds.min.y, combinedBounds.max.y),
                Mathf.Clamp(newPosition.z, combinedBounds.min.z, combinedBounds.max.z)
            );
        }

        private void LerpItem(PhysGrabCart cart, float t)
        {
            if (OriginalScale != baseTransform.localScale)
                baseTransform.localScale = Vector3.Lerp(baseTransform.localScale, OriginalScale, t);

            //if (EnemyBody != null)
            //{
            //    if (OriginalEnemyScale != EnemyBody.enemyParent.transform.localScale)
            //        EnemyBody.enemyParent.transform.localScale = Vector3.Lerp(EnemyBody.enemyParent.transform.localScale, OriginalEnemyScale, t);
            //}

            baseTransform.position = ClampToCartBounds(cart);
        }

        internal IEnumerator RestoreItem(PhysGrabCart cart)
        {
            Plugin.Spam($"{gameObject.name} position: {baseTransform.position}");

            //Enable item physics
            grabObj.isActive = true;
            isStored = false;
            baseTransform.localScale = transform.localScale;
            baseTransform.position = ClampToCartBounds(cart);

            //enable item lights
            if (itemLights.Count > 0)
                itemLights.Do(i => i.enabled = true);

            //wait for cart to stabilize
            float timer = 0f;
            while (timer < ModConfig.CartStabilizationTimer.Value)
            {
                timer += Time.deltaTime;
                LerpItem(cart, timer);
                yield return null;
            }

            //Enable colliders
            List<Collider> coll = [.. grabObj.gameObject.GetComponentsInChildren<Collider>()];
            coll.Do(c => c.enabled = true);

            //Set to original scale
            baseTransform.localScale = OriginalScale;

            //Enable Enemy stuff
            if (EnemyBody != null)
            {
                EnemyBody.enabled = true;
                if (EnemyBody.enemy.HasNavMeshAgent && SemiFunc.IsMasterClientOrSingleplayer())
                {
                    Plugin.Spam("Enabling navmesh");
                    EnemyBody.enemy.NavMeshAgent.Enable();
                    EnemyBody.enemy.EnemyTeleported(baseTransform.position);
                }
                yield return null;
                Plugin.Spam("Returning enemy state");
                EnemyBody.enemy.CurrentState = OriginalState;
            }

            //enable destructability
            grabObj.impactDetector.enabled = true;
            grabObj.rb.isKinematic = false;

            //enable map icon
            if (hasMapIcon)
            {
                if (mapIcon != null)
                    mapIcon.enabled = true;
                else
                    Plugin.WARNING($"Unable to show map icon for {gameObject.name}");
            }
           
            //last safety check
            yield return new WaitForSeconds(ModConfig.ItemSafetyTimer.Value);
            grabObj.impactDetector.isIndestructible = false;
            Plugin.Spam("RestoreItem complete!");
        }

    }
}
