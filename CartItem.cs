using System;
using UnityEngine;

namespace PocketCartPlus
{
    internal class CartItem
    {
        internal Vector3 PosOffset;
        //internal Quaternion QuartOffset;
        internal PhysGrabObject actualItem;
        internal bool isStored = false;

        internal CartItem(PhysGrabObject item, PhysGrabCart cart)
        {
            if (item == null || cart == null)
            {
                Plugin.ERROR("Unable to create CartItem off of null cart or item!");
                return;
            }

            PosOffset = item.transform.position - cart.inCart.position;
            //QuartOffset = Quaternion.Inverse(cart.inCart.rotation) * item.transform.rotation;
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

            PosOffset = cart.inCart.position - item.transform.position;
            //QuartOffset = Quaternion.Inverse(cart.inCart.rotation) * item.transform.rotation;
            actualItem = item;
            Plugin.Spam($"---------------\nCartItem updated to store {item.gameObject.name}'s offsets:\nPosOffset - {PosOffset}\n---------------");
        }

    }
}
