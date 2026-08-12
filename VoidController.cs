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
        //internal static readonly float basePriceMultiplier = 4f;
        private ItemAttributes ItemAtts { get; set; } = null!;
        private PhotonView PV { get; set; } = null!;

        public Renderer panelMesh = null!;
        public TMP_Text keypadDisplayText = null!;
        private Color lockedColor = new(0.75f, 0f, 0f, 1f); //custom red
        private Color unlockedColor = new(0.3f, 0.58f, 0.3f, 0f); //custom green
        private ItemToggle itemToggle = null!;
        private ItemEquippable itemEquippable = null!;
        private PhysGrabObject physGrabObject = null!;


        private void Awake()
        {
            Plugin.Spam("VoidController AWAKE");
            itemToggle = GetComponent<ItemToggle>();
            itemEquippable = GetComponent<ItemEquippable>();
            PV = GetComponent<PhotonView>();
            ItemAtts = GetComponent<ItemAttributes>();
            physGrabObject = GetComponent<PhysGrabObject>();
        }

        private void Start()
        {
            Plugin.Spam($"""
                panelMesh is null {panelMesh == null}
                keypadDisplayText is null {keypadDisplayText == null}

                """);
            // awake is too early
            UpdateCost();

            // sync remote with void status
            UpdateDisplay();
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

            UpdateDisplay();

            PV.RPC("SyncVoidStatus", RpcTarget.OthersBuffered, VoidIsLocked);
        }

        private void UpdateDisplay()
        {
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

        // only run on host client in the shop
        private void UpdateCost()
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer() || !SemiFunc.RunIsShop())
                return;

            // have to set min/max values divided by 4 because of how repo handles values for some reason
            ItemAtts.itemValueMin = HostValues.VRMinPrice.Value / 4f;
            ItemAtts.itemValueMax = HostValues.VRMaxPrice.Value / 4f;

            // update cost
            ItemAtts.GetValue();

            Plugin.Spam($"""
                Item attributes for {ItemAtts.itemName} updated with min price of {HostValues.VRMinPrice.Value} ({ItemAtts.itemValueMin}) and max price of {HostValues.VRMaxPrice.Value} ({ItemAtts.itemValueMax})
                Value is {ItemAtts.value}
                """);
        }
    }
}
