using HarmonyLib;
using Photon.Pun;
using System.Linq;
using UnityEngine;

namespace PocketCartPlus
{
    //class for managing network rpcs relating to each cart
    public class CartManager : MonoBehaviour
    {
        internal PhotonView photonView = null!;
        internal bool HasItems()
        {
            return EquipPatch.AllCartItems.Any(i => i.MyCart == MyCart);
        }
        internal bool isStoringItems = false;
        internal bool isShowingItems = false;
        internal static int CartsStoringItems = 0;
        internal PhysGrabCart MyCart = null!;
        internal string storedBy = string.Empty;
        internal static CartManager firstInstance;

        private static Color textColor = new(209f / 255f, 205f / 255f, 205f / 255f);

        private void Start()
        {
            Plugin.Spam("CartManager instance created!");
            photonView = gameObject.GetComponent<PhotonView>();
            MyCart = gameObject.GetComponent<PhysGrabCart>();
            if (firstInstance == null)
                firstInstance = this;
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
            if (storedBy == PlayerAvatar.instance.steamID)
            {
                CartsStoringItems++;
            }
               
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
