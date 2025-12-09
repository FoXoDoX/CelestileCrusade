using UnityEngine;

namespace My.Scripts.Gameplay.Pickups
{
    public class FuelPickup : MonoBehaviour
    {
        public void DestroySelf()
        {
            Destroy(gameObject);
        }
    }
}
