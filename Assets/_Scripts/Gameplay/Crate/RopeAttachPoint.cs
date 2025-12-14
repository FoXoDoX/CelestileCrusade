using UnityEngine;

namespace My.Scripts.Gameplay.Player
{
    /// <summary>
    /// Точка привязки верёвки. Следует за родителем, но имеет отдельный Rigidbody2D,
    /// чтобы ящик не влиял на физику Lander напрямую.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class RopeAttachPoint : MonoBehaviour
    {
        #region Private Fields

        private Rigidbody2D _rigidbody;
        private Transform _target;

        #endregion

        #region Properties

        public Rigidbody2D Rigidbody => _rigidbody;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _target = transform.parent;

            SetupRigidbody();
        }

        private void FixedUpdate()
        {
            FollowTarget();
        }

        #endregion

        #region Private Methods

        private void SetupRigidbody()
        {
            _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void FollowTarget()
        {
            if (_target == null) return;

            // Kinematic body просто перемещается в позицию родителя
            _rigidbody.MovePosition(_target.position);
        }

        #endregion
    }
}