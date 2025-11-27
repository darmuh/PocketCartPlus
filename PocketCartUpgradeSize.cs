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
        internal ItemAttributes itemAtts = null!;

        private void Awake()
        {
            itemAtts = gameObject.GetComponent<ItemAttributes>();
            Cart = gameObject.GetComponent<PhysGrabCart>();

            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;
            
            ChooseScale();

            UpgradeManager.PlusSizeCarts.RemoveAll(c => c == null);
            UpgradeManager.PlusSizeCarts.Add(this);
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

            valuePreset.valueMin = HostValues.PlusCartMinPrice.Value / basePriceMultiplier;
            valuePreset.valueMax = HostValues.PlusCartMaxPrice.Value / basePriceMultiplier;
            valuePreset.name = "pocketcartplus_value";

            Plugin.Spam($"valuePreset created for cartPlus upgrade with base min price of {HostValues.PlusCartMinPrice.Value} and base max price of {HostValues.PlusCartMaxPrice.Value}");
        }
    }
}
