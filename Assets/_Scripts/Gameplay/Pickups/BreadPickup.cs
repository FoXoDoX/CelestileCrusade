using UnityEngine;

namespace My.Scripts.Gameplay.Pickups
{

    public class BreadPickup : MonoBehaviour
    {
        public void DestroySelf()
        {
            Destroy(gameObject);
        }
    }
}
