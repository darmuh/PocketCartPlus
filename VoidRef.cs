using UnityEngine;
using System.Collections.Generic;

namespace PocketCartPlus
{
    public class VoidRef : MonoBehaviour
    {
        public GameObject inCart = null!;
        internal static List<CartItem> PlayersInVoid = [];
        float t = 0f;

        // for now all this does is check every 5 seconds if the only alive players are detected in the void
        // and then spits them out in the truck
        // maybe one day i'll make this something cooler, idk
        private void Update()
        {
            if (PlayersInVoid.Count == 0)
                return;

            t += Time.deltaTime;

            if (t > 10f)
            {
                int alive = SemiFunc.PlayerGetAll().FindAll(x => !x.isDisabled).Count;

                if (alive == PlayersInVoid.Count)
                {
                    Plugin.Spam($"Returning all alive ({alive}) players to truck as they are all detected in the void");
                    foreach (CartItem item in PlayersInVoid)
                    {
                        item.StartCoroutine(item.ReturnPlayerToLevel(TruckSafetySpawnPoint.instance.transform.position, item.playerRef.transform.rotation));
                        Plugin.Spam($"{item.playerRef.playerName} returned to truck");
                    }
                }

                t = 0f;
            }
        }
    }
}
