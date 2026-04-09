using UnityEngine;

namespace My.Scripts.Environment.Hazards
{
    [RequireComponent(typeof(TrailRenderer))]
    public class TrailWidthSetter : MonoBehaviour
    {
        [SerializeField] private float _startWidth = 3f;
        [SerializeField] private float _endWidth = 0f;

        private void Awake()
        {
            var trail = GetComponent<TrailRenderer>();
            trail.startWidth = _startWidth;
            trail.endWidth = _endWidth;
        }
    }
}