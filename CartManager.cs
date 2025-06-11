using HarmonyLib;
using Photon.Pun;
using System.Collections;
using UnityEngine;

namespace PocketCartPlus
{
    //class for managing network rpcs relating to each cart
    public class CartManager : MonoBehaviour
    {
        internal PhotonView photonView = null!;
        internal bool hasItems = false;
        internal bool isStoringItems = false;
        internal bool isShowingItems = false;
        internal int storedPlayers = 0;
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

        internal IEnumerator AbandonPlayerHint()
        {
            string messageText = "Hold <color=#f0bf30>[ALT]</color> to <color=#8E2E22>ABANDON</color> PLAYER";
            HintUI.instance.grabHint = false;
            while (storedPlayers > 0)
            {
                if(HintUI.instance.grabHint)
                {
                    yield return new WaitUntil(() => HintUI.instance.grabHint == false);
                    yield return new WaitUntil(() => HintUI.instance.messageTimer <= 0f);
                }

                if(HintUI.instance.transform.parent != InventoryUI.instance.transform)
                { 
                    HintUI.instance.transform.SetParent(InventoryUI.instance.transform);
                }

                HintUI.instance.ShowInfo($"{messageText}", textColor, 12f);
                HintUI.instance.Show();
                yield return null;
            }
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
            {
                CartsStoringItems++;
                //if(storedPlayers > 0)
                    //StartCoroutine(AbandonPlayerHint());
            }
               
        }

        [PunRPC]
        internal void ShowCartItems()
        {
            Plugin.Message("ShowCartItems detected!");

            Plugin.Spam("Starting cart coroutine!");
            MyCart.StartCoroutine(UpdateVisualsPatch.WaitToDisplay(this));
        }

        [PunRPC]
        private void ReceiveItemsUpgrade(int upgradeLevel)
        {
            if (!UpgradeManager.LocalItemsUpgrade)
                UpgradeManager.LocalItemsUpgrade = true;

            if (upgradeLevel != UpgradeManager.CartItemsUpgradeLevel)
                UpgradeManager.CartItemsUpgradeLevel = upgradeLevel;
        }
    }
}
