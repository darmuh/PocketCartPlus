using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PocketCartPlus
{
    internal class CartItem : MonoBehaviour
    {
        internal Vector3 PosOffset; //this is the local offset
        internal PhysGrabObject grabObj;
        internal bool isStored = false;
        internal Transform baseTransform;
        internal Vector3 OriginalScale;
        //internal Vector3 OriginalEnemyScale;

        internal bool isPlayer = false;
        internal PlayerAvatar playerRef = null!;

        internal EnemyRigidbody EnemyBody = null!;
        internal EnemyState OriginalState = EnemyState.None;
        internal List<Light> itemLights = [];

        internal PhysGrabCart MyCart = null!;
        //internal SpriteRenderer mapIcon = null!;
        //internal bool hasMapIcon = false;

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
            PlayerAvatar player;

            if (item.gameObject.transform.parent == null)
                player = null;
            else
                player = item.gameObject.transform.parent.GetComponentInChildren<PlayerAvatar>();

            if (item.isEnemy)
                return item.gameObject.GetComponent<EnemyRigidbody>().enemyParent.gameObject.transform;

            if (player != null)
            {
                isPlayer = true;
                playerRef = player;
                return player.gameObject.transform;
            }

            return item.gameObject.transform;
        }

        internal static void AddToEquip(PhysGrabObject item, PhysGrabCart cart)
        {        
            //Create/Update CartItem
            CartItem cartItem = item.gameObject.GetComponent<CartItem>() ?? item.gameObject.AddComponent<CartItem>();
            cartItem.UpdateItem(item, cart);

            cart.StartCoroutine(cartItem.EquipToVoid());
        }

        internal IEnumerator EquipToVoid()
        {
            bool isPlayer = playerRef != null;

            if (isPlayer && ModConfig.PlayerSafetyCheck.Value)
            {
                Plugin.Spam("Skipping player!");
                yield break;
            }

            if (grabObj.isEnemy && ModConfig.IgnoreEnemies.Value)
            {
                Plugin.Spam("Ignoring Enemy!");
                yield break;
            }

            if (isPlayer)
            {
                PocketDimension.TeleportPlayer(playerRef, PocketDimension.ThePocket.transform.position + new Vector3(0f, 5f * playerRef.transform.localScale.y - Random.Range(0f, 2f), 0f), PocketDimension.ThePocket.transform.rotation);
                Plugin.Spam($"Teleporting player to pocket dimension {PocketDimension.ThePocket.transform}");
                //Set stored
                isStored = true;
                MyCart.GetComponent<CartManager>().storedPlayers++;
                yield break;
            }

            //Disable item physics
            grabObj.rb.isKinematic = true;
            grabObj.isActive = false;
            grabObj.impactDetector.enabled = false;
            grabObj.impactDetector.isIndestructible = true;

            //Collider Disable
            List<Collider> coll = [.. baseTransform.gameObject.GetComponentsInChildren<Collider>()];
            coll.Do(c => c.enabled = false);


            //Enemy Disable
            if (EnemyBody != null && EnemyBody.enemy.MasterClient)
            {
                if (EnemyBody.enemy.HasNavMeshAgent && SemiFunc.IsMasterClientOrSingleplayer())
                    EnemyBody.enemy.NavMeshAgent.Stop(9999999f);

                EnemyBody.frozen = true;
            }

            yield return EquipPatch.ChangeSize(0.2f, baseTransform.localScale * 0.01f, baseTransform.localScale, baseTransform);

            //Lights Disable
            if (itemLights.Count > 0)
                itemLights.Do(i => i.enabled = false);

            if(EnemyBody != null)
            {
                //Set stored
                isStored = true;
                Plugin.Spam($"{gameObject.name} has been equipped with the cart!");
                yield break;
            }

            PocketCartUpgradeSize isPlus = MyCart.gameObject.GetComponent<PocketCartUpgradeSize>();
            Vector3 LocalOffset = PosOffset;

            if (isPlus != null)
                LocalOffset /= isPlus.chosenScale;

            Vector3 scaledLocalOffset = PosOffset * 40f;
            Vector3 buffer = new(1f, 1f, 1f);



            baseTransform.position = PocketDimension.voidRef.inCart.transform.position + scaledLocalOffset + buffer;
            
            yield return null;
            
            if(!gameObject.GetComponent<PlayerDeathHead>())
                yield return EquipPatch.ChangeSize(0.2f, OriginalScale * 40, baseTransform.localScale, baseTransform);

            coll.Do(c => c.enabled = true);

            //re-enable lights
            if (itemLights.Count > 0)
                itemLights.Do(i => i.enabled = true);

            //Set stored
            isStored = true;

            Plugin.Spam($"{gameObject.name} has been equipped with the cart!");
        }

        internal Vector3 ClampToCartBounds(PhysGrabCart cart)
        {
            Vector3 newPosition = cart.inCart.position + new Vector3(
                PosOffset.x * cart.inCart.localScale.x,
                PosOffset.y * cart.inCart.localScale.y,
                PosOffset.z * cart.inCart.localScale.z
            );
            Bounds combinedBounds = cart.capsuleColliders[0].bounds;

            combinedBounds.min += new Vector3(0.2f, 0.6f, 0.2f);
            combinedBounds.max -= new Vector3(0.2f, -0.6f, 0.2f);

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

            baseTransform.position = ClampToCartBounds(cart);
        }

        internal IEnumerator RestoreItem(PhysGrabCart cart)
        {
            Plugin.Spam($"{gameObject.name} position: {baseTransform.position}");
            bool isPlayer = playerRef != null;

            //Collider Disable
            List<Collider> coll = [.. baseTransform.gameObject.GetComponentsInChildren<Collider>()];
            if (!isPlayer)
                coll.Do(c => c.enabled = false);

            yield return null;

            if (isPlayer)
            {
                if (playerRef.isDisabled)
                    baseTransform = playerRef.playerDeathHead.transform;
                else
                {
                    PocketDimension.TeleportPlayer(playerRef, cart.inCart.position + new Vector3(0f, 1f, 0f), cart.inCart.rotation);
                    Plugin.Spam($"Teleporting player back to level {cart.inCart.position}");
                    isStored = false;
                    cart.GetComponent<CartManager>().storedPlayers = Mathf.Clamp(MyCart.GetComponent<CartManager>().storedPlayers - 1, 0, 99);
                    yield break;
                }
            }

            baseTransform.localScale = OriginalScale * 0.1f;

            //Enable item physics
            grabObj.isActive = true;
            isStored = false;

            baseTransform.localScale = transform.localScale;
            baseTransform.position = ClampToCartBounds(cart);

            //wait for cart to stabilize
            float timer = 0f;
            while (timer < ModConfig.CartStabilizationTimer.Value)
            {
                timer += Time.deltaTime;
                LerpItem(cart, timer);
                yield return null;
            }

            //Enable colliders
            if(!isPlayer)
                coll.Do(c => c.enabled = true);

            //Set to original scale
            baseTransform.localScale = OriginalScale;

            //Enable Enemy stuff
            if (EnemyBody != null && EnemyBody.enemy.MasterClient)
            {
                EnemyBody.enabled = true;
                if (EnemyBody.enemy.HasNavMeshAgent && SemiFunc.IsMasterClientOrSingleplayer())
                {
                    Plugin.Spam("Enabling navmesh");
                    EnemyBody.enemy.NavMeshAgent.StopTimer = 0f;
                    EnemyBody.frozen = false;
                    EnemyBody.enemy.EnemyTeleported(baseTransform.position);
                }
                yield return null;
                Plugin.Spam("Returning enemy state");
                EnemyBody.enemy.CurrentState = OriginalState;
            }

            //enable destructability
            grabObj.impactDetector.enabled = true;
            grabObj.rb.isKinematic = false;

           
            //last safety check
            yield return new WaitForSeconds(ModConfig.ItemSafetyTimer.Value);
            grabObj.impactDetector.isIndestructible = false;
            Plugin.Spam("RestoreItem complete!");
        }

    }
}
