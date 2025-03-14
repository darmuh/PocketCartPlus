using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PocketCartPlus
{
    internal class CartItem
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

        internal CartItem(PhysGrabObject item, PhysGrabCart cart)
        {
            if (item == null || cart == null)
            {
                Plugin.ERROR("Unable to create CartItem off of null cart or item!");
                return;
            }

            Plugin.Spam($"cart scale: {cart.inCart.localScale}");

            PosOffset = item.transform.position - cart.inCart.position;
            OriginalScale = item.transform.localScale;

            itemLights = [.. item.gameObject.GetComponentsInChildren<Light>()];

            if (item.isEnemy)
            {
                EnemyBody = item.gameObject.GetComponent<EnemyRigidbody>();
                OriginalEnemyScale = EnemyBody.enemyParent.transform.localScale;
                OriginalState = EnemyBody.enemy.CurrentState;
                List<Light> enemyLights = [.. EnemyBody.enemyParent.gameObject.GetComponentsInChildren<Light>()];

                if (enemyLights.Count > 0)
                    itemLights.AddRange(enemyLights);
            }

            itemLights.RemoveAll(i => i.enabled = false);

            //calculate scale
            PosOffset = new Vector3(
                PosOffset.x / cart.inCart.localScale.x,
                PosOffset.y / cart.inCart.localScale.y,
                PosOffset.z / cart.inCart.localScale.z
                );
            // Save the rotational offset
            //QuartOffset = Quaternion.Inverse(cart.transform.rotation) * item.transform.rotation;
            Plugin.Spam($"---------------\nCartItem created to store {item.gameObject.name}'s offsets:\nPosOffset - {PosOffset}\n---------------");
            actualItem = item;

            EquipPatch.AllCartItems.Add(this);
        }

        internal void UpdateItem(PhysGrabObject item, PhysGrabCart cart)
        {
            if (item == null || cart == null)
            {
                Plugin.ERROR("Unable to update CartItem off of null cart or item!");
                return;
            }

            PosOffset = item.transform.position - cart.inCart.position;
            OriginalScale = item.transform.localScale;

            itemLights = [.. item.gameObject.GetComponentsInChildren<Light>()];

            if (item.isEnemy)
            {
                EnemyBody = item.gameObject.GetComponent<EnemyRigidbody>();
                OriginalEnemyScale = EnemyBody.enemyParent.transform.localScale;
                OriginalState = EnemyBody.enemy.CurrentState;

                List<Light> enemyLights = [.. EnemyBody.enemyParent.gameObject.GetComponentsInChildren<Light>()];

                if (enemyLights.Count > 0)
                    itemLights.AddRange(enemyLights);
            }

            itemLights.RemoveAll(i => i.enabled = false);

            //calculate scale
            PosOffset = new Vector3(
                PosOffset.x / cart.inCart.localScale.x,
                PosOffset.y / cart.inCart.localScale.y,
                PosOffset.z / cart.inCart.localScale.z
                );
            //QuartOffset = Quaternion.Inverse(cart.transform.rotation) * item.transform.rotation;
            actualItem = item;
            Plugin.Spam($"---------------\nCartItem updated to store {item.gameObject.name}'s offsets:\nPosOffset - {PosOffset}\n---------------");
        }

        internal static void HideCartItems(object cartObj)
        {
            Plugin.Spam("HideCartItems detected!");
            int cartIndex = (int)cartObj;

            if (GetPocketCarts.AllSmallCarts.Count < cartIndex || cartIndex < 0)
            {
                Plugin.WARNING($"cartIndex is invalid! [ {cartIndex} ]");
                return;
            }
                
            PhysGrabCart cart = GetPocketCarts.AllSmallCarts.ElementAt(cartIndex);
            cart.itemsInCart.Do(c =>
            {
                EquipPatch.AddToEquip(c, cart);
            });

            EquipPatch.AllCartItems.RemoveAll(c => c.actualItem == null);
        }

        internal static void ShowCartItems(object cartObj)
        {
            Plugin.Spam("ShowCartItems detected!");
            int cartIndex = (int)cartObj;

            if (GetPocketCarts.AllSmallCarts.Count < cartIndex || cartIndex < 0)
            {
                Plugin.WARNING($"cartIndex is invalid! [ {cartIndex} ]");
                return;
            }

            PhysGrabCart cart = GetPocketCarts.AllSmallCarts.ElementAt(cartIndex);
            cart.StartCoroutine(UpdateVisualsPatch.WaitToDisplay(cart));
        }

    }
}
