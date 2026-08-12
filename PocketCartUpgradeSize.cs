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
        public PhysGrabCart Cart = null!;
        public float chosenScale = 1.25f;
        public Vector3 chosenVector3;
        private ItemAttributes ItemAtts { get; set; } = null!;

        private void Awake()
        {
            Plugin.Spam("PocketCartUpgradeSize AWAKE");
            ItemAtts = gameObject.GetComponent<ItemAttributes>();
            Cart = gameObject.GetComponent<PhysGrabCart>();
         
            ChooseScale();

            UpgradeManager.PlusSizeCarts.RemoveAll(c => c == null);
            UpgradeManager.PlusSizeCarts.Add(this);
        }

        private void Start()
        {
            UpdateName();

            // awake is too early
            UpdateCost();
        }

        private void ChooseScale()
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;

            Plugin.Spam("Chosing scale!");
            int rarity = Plugin.Rand.Next(0, 100);

            if (rarity < 75)
                chosenScale = 1.25f;
            else if (rarity > 95)
                chosenScale = 1.75f;
            else
                chosenScale = 1.5f;

            if (SemiFunc.RunIsShop() || !HostValues.PlusCartRareVariants.Value)
                chosenScale = 1.25f;

            if (SemiFunc.IsMultiplayer())
                photonView.RPC("SyncScale", RpcTarget.AllBuffered, chosenScale);
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
                ItemAtts.itemName = "POCKET C.A.R.T. PLUS2";

            if(chosenScale == 1.75f)
                ItemAtts.itemName = "POCKET C.A.R.T. PLUS3";
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

        // only run on host client in the shop
        private void UpdateCost()
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer() || !SemiFunc.RunIsShop())
                return;

            // have to set min/max values divided by 4 because of how repo handles values for some reason
            ItemAtts.itemValueMin = HostValues.PlusCartMinPrice.Value / 4f;
            ItemAtts.itemValueMax = HostValues.PlusCartMaxPrice.Value / 4f;

            // update cost
            ItemAtts.GetValue();

            Plugin.Spam($"""
                Item attributes for {ItemAtts.itemName} updated with min price of {HostValues.PlusCartMinPrice.Value} ({ItemAtts.itemValueMin}) and max price of {HostValues.PlusCartMaxPrice.Value} ({ItemAtts.itemValueMax})
                Value is {ItemAtts.value}
                """);
        }
    }
}
