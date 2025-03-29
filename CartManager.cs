using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace PocketCartPlus
{
    //class for managing network rpcs relating to each cart
    internal class CartManager : MonoBehaviour
    {
        internal PhotonView photonView = null!;
        internal bool hasItems = false;
        internal bool isStoringItems = false;
        internal bool isShowingItems = false;
        internal static int CartsStoringItems = 0;
        internal PhysGrabCart MyCart = null!;
        internal string storedBy = string.Empty;

        private void Start()
        {
            Plugin.Spam("CartManager instance created!");
            photonView = gameObject.GetComponent<PhotonView>();
            MyCart = gameObject.GetComponent<PhysGrabCart>();
        }

        private void Destroy()
        {
            Plugin.Spam("CartManager destroyed!");
        }

        [PunRPC]
        internal void HideCartItems(string steamID)
        {
            if (isStoringItems)
                return;

            storedBy = steamID;
            isStoringItems = true;
            Plugin.Spam("HideCartItems detected!");

            MyCart.itemsInCart.Do(c =>
            {
                Plugin.Spam($"AddToEquip [ {c.gameObject.name} ]");
                CartItem.AddToEquip(c, MyCart);
            });

            EquipPatch.AllCartItems.RemoveAll(c => c.grabObj == null);
            isStoringItems = false;
            hasItems = true;
            if (storedBy == PlayerAvatar.instance.steamID)
                CartsStoringItems++;
        }

        [PunRPC]
        internal void ShowCartItems()
        {
            Plugin.Message("ShowCartItems detected!");

            Plugin.Spam("Starting cart coroutine!");
            MyCart.StartCoroutine(UpdateVisualsPatch.WaitToDisplay(this));
        }
    }
}
