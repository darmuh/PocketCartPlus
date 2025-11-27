using BepInEx.Configuration;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PocketCartPlus
{
    public interface IValueSetter
    {
        void SetValue(bool sync);
    }

    internal abstract class HostConfigBase
    {
        internal static List<HostConfigBase> HostConfigItems = [];
        internal static List<string> namesToSync = [];
        internal static bool HostConfigInit = false;
        internal abstract Type ConfigType { get; } 
        internal abstract object BoxedVal { get; }
        internal abstract string Name { get; }
        internal abstract bool RequireSync { get; }
        internal TaskCompletionSource<bool> areValuesReady = new();
        internal static TaskCompletionSource<bool> isSyncReady = new();

        protected HostConfigBase()
        {
            HostConfigItems.Add(this);
        }

        public static void SyncIsReady()
        {
            if (SemiFunc.IsMasterClientOrSingleplayer())
                return;

            Plugin.Spam($"Asking host for their config items");
            //Photon.Realtime.Player host = GameDirector.instance.PlayerList.Find(p => p.photonView.Controller.IsMasterClient).photonView.Owner;
            GlobalNetworking.Instance.photonView.RPC("AskHostAll", PhotonNetwork.MasterClient, (object)namesToSync.ToArray<string>());

            isSyncReady.TrySetResult(true);
            isSyncReady = new();
        }

    }

    internal class HostConfigItem<T>(ConfigEntry<T> entry, bool requireSync = true) : HostConfigBase, IValueSetter
    {
        internal override Type ConfigType
        {
            get
            {
                return typeof(T);
            }
        }

        internal ConfigEntry<T> configItem = entry;
        internal override bool RequireSync
        {
            get
            {
                return requireSync;
            }
        }

        internal override string Name
        {
            get => configItem.Definition.Key;
        }
        internal override object BoxedVal
        {
            get => configItem.BoxedValue;
        }

        internal T Value = default!;

        public static HostConfigItem<T> GetItem(string id)
        {
            HostConfigBase item = HostConfigItems.Find(c => c.Name == id);
            if (item.BoxedVal.GetType() != typeof(T))
                throw new InvalidCastException($"Config item is not of type {typeof(T)}");

            return (HostConfigItem<T>)item;
        }

        void IValueSetter.SetValue(bool sync)
        {
            Plugin.Spam($"Setting value of {Name}");
            if (!SpawnPlayerStuff.AreWeInGame() || !sync)
            {
                Value = configItem.Value;
                return;
            }
                
            if (SemiFunc.IsMasterClientOrSingleplayer())
                Value = configItem.Value;
            else
            {
                _ = GetValueFromHost();
            }
        }

        private async Task<T> GetValueFromHost()
        {
            Plugin.Spam($"Adding item to sync: {Name}");
            namesToSync.Add(Name);

            //wait for all names from list to be added
            await isSyncReady.Task;
            
            //wait for this config item's value to be ready
            await areValuesReady.Task;

            Plugin.Spam($"Got value from host {Value}");
            return Value;
        }

        public void ValueIsReady()
        {
            areValuesReady.TrySetResult(true);
            areValuesReady = new();
        }


    }
}
