using UnityEngine;

namespace My.Scripts.Environment.Background
{
    public class ParallaxBackground : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float parallaxFactor = 0.025f;

        private Material backgroundMaterial;
        private Vector3 startTargetPosition;
        private static readonly int ParallaxOffset = Shader.PropertyToID("_ParallaxOffset");

        void Start()
        {
            Renderer renderer = GetComponent<Renderer>();
            backgroundMaterial = renderer.material;

            startTargetPosition = target.position;
        }

        void Update()
        {
            Vector3 baseMovement = target.position;
            transform.position = new Vector3(baseMovement.x, baseMovement.y, transform.position.z);

            Vector3 targetDelta = target.position - startTargetPosition;
            Vector2 parallaxOffset = new Vector2(
                -targetDelta.x * parallaxFactor,
                -targetDelta.y * parallaxFactor
            );

            if (backgroundMaterial.HasProperty(ParallaxOffset))
            {
                backgroundMaterial.SetVector(ParallaxOffset, parallaxOffset);
            }
        }
    }
}