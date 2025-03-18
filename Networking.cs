using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using REPOLib.Modules;
using System.Linq;

namespace PocketCartPlus
{
    public class Networking
    {
        internal static NetworkedEvent HideCartItems;
        internal static NetworkedEvent ShowCartItems;
        internal static NetworkedEvent UnlockUpgrade;
        internal static NetworkedEvent HostCheck;

        //Unlocks
        internal static readonly string CartItemsUpgrade = "CartItems";
        internal static readonly string CartSizeUpgrade = "CartSize";

        internal static void Init()
        {
            HideCartItems ??= new("HideCartItems", CartItem.HideCartItems);
            ShowCartItems ??= new("ShowCartItems", CartItem.ShowCartItems);
            UnlockUpgrade ??= new("Unlock Pocket Cart Upgrade", SetUnlockedStatus);
            HostCheck ??= new("Unlock Cart Items Upgrade", CheckUnlockedStatus);
        }

        internal static void CheckUnlockedStatus(EventData eventData)
        {
            string upgradeName = (string)eventData.CustomData;

            if (!PhotonNetwork.IsMasterClient)
            {
                if (upgradeName == CartItemsUpgrade)
                    PocketCartUpgradeItems.FromHost();
  
                return;
            }

            Player client = PhotonNetwork.PlayerList.FirstOrDefault(p => p.ActorNumber == eventData.Sender);
            string steamID = client.UserId;

            if (upgradeName == CartItemsUpgrade)
            {
                if (eventData.Sender == PhotonNetwork.MasterClient.ActorNumber)
                    PocketCartUpgradeItems.FromHost();
                else
                    PocketCartUpgradeItems.AskHost(eventData, steamID);
            }
                
        }

        //Upgrade was unlocked by a client
        internal static void SetUnlockedStatus(EventData eventData)
        {
            Plugin.Message("NETWORK EVENT: SetUnlockedStatus");
            string upgradeName = (string)eventData.CustomData;
            Plugin.Spam($"upgradeName - [ {upgradeName} ]");

            if (upgradeName == CartItemsUpgrade)
                PocketCartUpgradeItems.UpdateStatus(eventData);
        }
    }
}
