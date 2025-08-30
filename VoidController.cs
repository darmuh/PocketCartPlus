using Photon.Pun;
using System.Linq;
using TMPro;
using UnityEngine;

namespace PocketCartPlus
{
    public class VoidController : MonoBehaviour
    {

        internal static bool VoidIsLocked { get; private set; } = false;

        internal static float DistanceFromFace = 0.75f;
        internal static Quaternion xTurn = Quaternion.Euler(0f, 0f, 0f);
        internal static Quaternion yTurn = Quaternion.Euler(0f, 350f, 0f);
        internal static readonly float basePriceMultiplier = 4f;
        internal static Value valuePreset = null!;
        internal ItemAttributes itemAtts = null!;
        internal PhotonView photonView;

        public Renderer panelMesh;
        public TMP_Text keypadDisplayText;
        private Color lockedColor = new Color(0.75f, 0f, 0f, 1f); //custom red
        private Color unlockedColor = new Color(0.3f, 0.58f, 0.3f, 0f); //custom green
        private ItemToggle itemToggle;
        private ItemEquippable itemEquippable;
        private PhysGrabObject physGrabObject;


        private void Awake()
        {
            itemToggle = GetComponent<ItemToggle>();
            itemEquippable = GetComponent<ItemEquippable>();
            photonView = GetComponent<PhotonView>();
            itemAtts = GetComponent<ItemAttributes>();
            physGrabObject = GetComponent<PhysGrabObject>();
        }

        private void Physics()
        {
            if (!physGrabObject.grabbed || !physGrabObject.grabbedLocal)
                return;

            bool pushedOrPulled = (PhysGrabber.instance.isPulling || PhysGrabber.instance.isPushing);

            //float dist = 0.6f;

            if (!pushedOrPulled)
            {
                PhysGrabber.instance.OverrideGrabDistance(DistanceFromFace);

                if (!physGrabObject.isRotating)
                {
                    Quaternion identity = Quaternion.identity;
                    physGrabObject.TurnXYZ(xTurn, yTurn, identity); //face screen towards player
                }
            }
        }

        private void Update()
        {
            Physics();

            if (SemiFunc.RunIsShop() || itemEquippable.isEquipped)
                return;

            if (VoidIsLocked == itemToggle.toggleState)
                return;

            VoidIsLocked = itemToggle.toggleState;

            if (VoidIsLocked)
            {
                panelMesh.material.SetVector("_EmissionColor", lockedColor);
                keypadDisplayText.text = "LOCKED";
            }
            else
            {
                panelMesh.material.SetVector("_EmissionColor", unlockedColor);
                keypadDisplayText.text = "UNLOCKED";
            }

            photonView.RPC("SyncVoidStatus", RpcTarget.OthersBuffered, VoidIsLocked);
        }

        [PunRPC]
        private void SyncVoidStatus(bool status)
        {
            if(itemToggle.toggleState == status) 
                return;
            else
            {
                itemToggle.toggleState = status;
            }
        }

        internal static void ValueRef()
        {
            if (valuePreset == null)
                valuePreset = ScriptableObject.CreateInstance<Value>();

            valuePreset.valueMin = HostValues.VRMinPrice.Value / basePriceMultiplier;
            valuePreset.valueMax = HostValues.VRMaxPrice.Value / basePriceMultiplier;
            valuePreset.name = "voidRemote_value";

            Plugin.Spam($"valuePreset created for voidRemote item with base min price of {HostValues.VRMinPrice.Value} and base max price of {HostValues.VRMaxPrice.Value}");
        }

        internal static void ShopPatch()
        {
            bool shouldAdd = false;

            Item voidRemote = REPOLib.Modules.Items.GetItemByName("Item VoidRemote");

            if (voidRemote == null)
            {
                Plugin.Spam($"Item not found!");
                return;
            }

            if (HostValues.VRRarity.Value >= Plugin.Rand.Next(0, 100))
                shouldAdd = true;

            if (!shouldAdd && ShopManager.instance.potentialSecretItems.Any(x => x.Value.Contains(voidRemote)))
            {
                int CountToReplace = ShopManager.instance.potentialSecretItems.Values.Sum(x => x.Count(i => i.itemAssetName == voidRemote.itemAssetName));
                Plugin.Spam($"Add-on rarity has determined {voidRemote.itemName} should be removed from the store! Original contains {CountToReplace} of this item");
                foreach (var item in ShopManager.instance.potentialSecretItems.Values)
                {
                    item.RemoveAll(i => i.itemAssetName == voidRemote.itemAssetName);
                }
            }

            Plugin.Spam($"Rarity determined void remote is valid to be added to the shop {shouldAdd}");
            voidRemote.value = valuePreset;
            Plugin.Spam($"Value preset set for void remote!");
        }
    }
}
