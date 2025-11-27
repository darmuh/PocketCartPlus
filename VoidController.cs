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
        internal PhotonView photonView = null!;

        public Renderer panelMesh = null!;
        public TMP_Text keypadDisplayText = null!;
        private Color lockedColor = new(0.75f, 0f, 0f, 1f); //custom red
        private Color unlockedColor = new(0.3f, 0.58f, 0.3f, 0f); //custom green
        private ItemToggle itemToggle = null!;
        private ItemEquippable itemEquippable = null!;
        private PhysGrabObject physGrabObject = null!;


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
    }
}
