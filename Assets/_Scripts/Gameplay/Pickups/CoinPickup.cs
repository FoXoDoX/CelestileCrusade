using UnityEngine;

namespace My.Scripts.Gameplay.Pickups
{

    public class CoinPickup : MonoBehaviour
    {
        public void DestroySelf()
        {
            Destroy(gameObject);
        }
    }
}
