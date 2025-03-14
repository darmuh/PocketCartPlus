using ExitGames.Client.Photon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PocketCartPlus
{
    public class NetworkedEvent
    {
        internal Action<object> EventAction;
        internal byte EventByte;
        internal string Name;

        public NetworkedEvent(string name, Action<object> eventAction)
        {
            Name = name;
            EventAction = eventAction;
            EventByte = GetUniqueID();
            Networking.AllCustomEvents.Add(this);
        }

        private static byte GetUniqueID()
        {
            byte id = 1;
            do
            {
                id++;
            } while (Networking.AllEvents.Any(e => e.Code == id) || Networking.AllCustomEvents.Any(u => u.EventByte == id));

            return id;
        }
    }
}
