using TMPro;
using UnityEngine;

namespace My.Scripts.Gameplay.LandingPads
{
    public class LandingPadVisual : MonoBehaviour
    {
        [SerializeField] private TextMeshPro scoreMultiplierTextMesh;

        private void Awake()
        {
            LandingPad landingPad = GetComponent<LandingPad>();
            scoreMultiplierTextMesh.text = "x" + landingPad.ScoreMultiplier;
        }
    }
}
