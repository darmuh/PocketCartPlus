using Photon.Pun;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace PocketCartPlus
{
    public class PocketCartUpgradeSize : MonoBehaviourPunCallbacks
    {
        internal static Value valuePreset = null!;
        internal static readonly float basePriceMultiplier = 4f;
        public float chosenScale = 1.25f;
        public Vector3 chosenVector3;
        internal ItemAttributes itemAtts = null!;

        private void Awake()
        {
            itemAtts = gameObject.GetComponent<ItemAttributes>();

            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;
            
            ChooseScale();      
        }

        private void Start()
        {
            UpdateName();
        }

        private void ChooseScale()
        {
            Plugin.Spam("Chosing scale!");
            int rarity = Plugin.Rand.Next(0, 100);

            if (rarity < 75)
                chosenScale = 1.25f;
            else if (rarity > 95)
                chosenScale = 1.75f;
            else
                chosenScale = 1.5f;

            if (SemiFunc.RunIsShop())
                chosenScale = 1.25f;

            if (SemiFunc.IsMultiplayer())
                photonView.RPC("SyncScale", RpcTarget.All, chosenScale);
            else
                SyncScale(chosenScale);
        }

        [PunRPC]
        internal void SyncScale(float scale)
        {
            Plugin.Spam($"Syncing scale of {scale}");
            chosenScale = scale;
            chosenVector3 = new Vector3(scale, scale, scale);
            base.transform.localScale = chosenVector3;
        }

        private void UpdateName()
        {
            Plugin.Spam("Updating Name!");
            if (chosenScale == 1.5f)
                itemAtts.itemName = "POCKET C.A.R.T. PLUS2";

            if(chosenScale == 1.75f)
                itemAtts.itemName = "POCKET C.A.R.T. PLUS3";
        }

        internal void ReturnScale()
        {
            StartCoroutine(ReturnToSize());
        }

        private IEnumerator ReturnToSize()
        {
            while (base.transform.localScale != Vector3.one)
                yield return null;

            yield return EquipPatch.ChangeSize(0.2f, chosenVector3, base.transform.localScale, base.transform);
            Plugin.Spam($"Scale has been returned to chosen scale {chosenScale}");
        }

        internal static void ValueRef()
        {
            if (valuePreset == null)
                valuePreset = ScriptableObject.CreateInstance<Value>();

            valuePreset.valueMin = ModConfig.PlusItemMinPrice.Value / basePriceMultiplier;
            valuePreset.valueMax = ModConfig.PlusItemMaxPrice.Value / basePriceMultiplier;
            valuePreset.name = "pocketcartplus_value";

            Plugin.Spam($"valuePreset created for cartPlus upgrade with base min price of {ModConfig.PlusItemMinPrice.Value} and base max price of {ModConfig.PlusItemMaxPrice.Value}");
        }

        internal static void ShopPatch()
        {
            bool shouldAdd = false;

            Item cartPlus = ShopManager.instance.potentialItems.FirstOrDefault(i => i.itemAssetName == "Item Cart Small Plus");

            if (cartPlus == null)
            {
                Plugin.Spam($"Item not found in potentialItems ({ShopManager.instance.potentialItems.Count})!");
                return;
            }

            if (ModConfig.PlusItemRarity.Value >= Plugin.Rand.Next(0, 100))
                shouldAdd = true;

            if (!shouldAdd)
                ShopManager.instance.potentialItems.Remove(cartPlus);

            Plugin.Spam($"Rarity determined item is a valid potential itemUpgrade in the shop {shouldAdd}");
            cartPlus.value = valuePreset;
            Plugin.Spam($"Value preset set for cart small plus!");
        }
    }
}
