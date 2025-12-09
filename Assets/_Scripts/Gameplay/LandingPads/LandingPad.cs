using UnityEngine;

namespace My.Scripts.Gameplay.LandingPads
{
    public class LandingPad : MonoBehaviour
    {
        [SerializeField] private int scoreMultiplier;

        public int ScoreMultiplier => scoreMultiplier;
    }
}
