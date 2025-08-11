using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PocketCartPlus
{
    internal class GlobalNetworking : MonoBehaviour
    {
        internal PhotonView photonView = null!;
        internal static GlobalNetworking Instance
        {
            get
            {
                if(RunManager.instance.runManagerPUN.gameObject.GetComponent<GlobalNetworking>() == null)
                    RunManager.instance.runManagerPUN.gameObject.AddComponent<GlobalNetworking>();

                return RunManager.instance.runManagerPUN.gameObject.GetComponent<GlobalNetworking>();
            }
        }

        private void Awake()
        {
            Plugin.Spam("GlobalNetworking instance created!");
            photonView = gameObject.GetComponent<PhotonView>();
        }

        [PunRPC]
        internal void AskHostAll(string[] names, PhotonMessageInfo info)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;

            Photon.Realtime.Player asker = info.Sender;

            Dictionary<string, bool> bools = [];
            Dictionary<string, float> floats = [];
            Dictionary<string, int> ints = [];
            foreach (string id in names)
            {
                Plugin.Spam($"Host is being asked for value of {id}");
                HostConfigBase valBase = HostConfigBase.HostConfigItems.Find(c => c.Name == id);
                if (valBase.BoxedVal is bool)
                {
                    bool value = HostConfigItem<bool>.GetItem(id).Value;
                    bools.Add(id, value);
                }
                else if (valBase.BoxedVal is float)
                {
                    float value = HostConfigItem<float>.GetItem(id).Value;
                    floats.Add(id, value);
                }
                else if (valBase.BoxedVal is int)
                {
                    int value = HostConfigItem<int>.GetItem(id).Value;
                    ints.Add(id, value);
                }
                else
                {
                    Plugin.ERROR($"Unexpected type in AskHostValue! {id}\nFrom {asker.NickName}");
                }
            }

            photonView.RPC("HostSendAll", asker, [bools.Keys.ToArray<string>(), bools.Values.ToArray<bool>(), floats.Keys.ToArray<string>(), floats.Values.ToArray<float>(), ints.Keys.ToArray<string>(), ints.Values.ToArray<int>()]);
        }

        [PunRPC]
        internal void HostSendIndividual(string id, object value)
        {
            if(value is bool boolVal)
                HostSent<bool>([id], [boolVal]);
            else if(value is float floatVal)
                HostSent<float>([id], [floatVal]);
            else if(value is int intVal)
                HostSent<int>([id], [intVal]);
            else
            {
                Plugin.WARNING($"Invalid type at HostSendIndividual for {id}!");
            }
        }

        [PunRPC]
        internal void HostSendAll(string[] boolNames, bool[] boolValues, string[] floatNames, float[] floatValues, string[] intNames, int[] intValues)
        {
            if (SemiFunc.IsMasterClientOrSingleplayer())
                return;

            HostSent<bool>(boolNames, boolValues);
            HostSent<float>(floatNames, floatValues);
            HostSent<int>(intNames, intValues);
        }

        internal void HostSent<T>(string[] names, T[] values)
        {
            if (names.Length != values.Length)
            {
                Plugin.ERROR($"Invalid value count!\nNames: {names.Length}\nValues: {values.Length}");
                return;
            }

            for (int i = 0; i < names.Length; i++)
            {
                Plugin.Message($"Updating value of {names[i]} to {values[i]}");

                HostConfigItem<T> item = HostConfigItem<T>.GetItem(names[i]);
                item.Value = values[i];
                item.ValueIsReady();
            }
        }

        [PunRPC]
        internal void ReceiveItemsUpgrade(int upgradeLevel)
        {
            if (!UpgradeManager.LocalItemsUpgrade)
                UpgradeManager.LocalItemsUpgrade = true;

            if (upgradeLevel != UpgradeManager.CartItemsUpgradeLevel)
                UpgradeManager.CartItemsUpgradeLevel = upgradeLevel;
        }
    }
}
