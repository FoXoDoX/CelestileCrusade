using UnityEngine;
using System;

public class RopeWithCrate : MonoBehaviour
{
    [SerializeField] private GameObject anchor;
    [SerializeField] private GameObject crate;

    public static RopeWithCrate Instance { get; private set; }

    private bool isDestroyed = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        AddCrateCollisionHandler();

        if (!isDestroyed)
        {
            Collider2D landerCollider = Lander.Instance.GetComponent<Collider2D>();
            Collider2D[] ropeColliders = GetComponentsInChildren<Collider2D>();
            Collider2D crateCollider = crate.GetComponent<Collider2D>();

            foreach (Collider2D ropeChainColliders in ropeColliders)
            {
                if (ropeChainColliders == crateCollider) continue;

                Physics2D.IgnoreCollision(landerCollider, ropeChainColliders);
                Physics2D.IgnoreCollision(crateCollider, ropeChainColliders);
            }

            CrateOnRope.Instance.OnCrateDrop += CrateOnRope_OnCrateDrop;
        }

        SoundManager.Instance.RopeWithCrateSpawned();
        GameManager.Instance.RopeWithCrateSpawned();
    }

    private void AddCrateCollisionHandler()
    {
        if (crate != null)
        {
            var crateCollisionHandler = crate.AddComponent<CrateOnRope>();
            crateCollisionHandler.OnCrateCollider += HandleCrateCollision;
        }
    }

    private void HandleCrateCollision(Collider2D collider2D)
    {
        DestroySelf();
    }

    private void CrateOnRope_OnCrateDrop(object sender, EventArgs e)
    {
        DestroySelf();
    }

    void FixedUpdate()
    {
        if (isDestroyed) return;

        if (IsValidAnchor())
        {
            UpdatePosition();
        }
    }

    private void UpdatePosition()
    {
        anchor.transform.position = Lander.Instance.transform.position - Vector3.up;
    }

    private bool IsValidAnchor()
    {
        if (anchor == null) return false;
        if (isDestroyed) return false;

        if (anchor == null) return false;

        HingeJoint2D joint = anchor.GetComponent<HingeJoint2D>();
        return joint != null;
    }

    private void DestroySelf()
    {
        if (isDestroyed) return;

        isDestroyed = true;

        Lander.Instance.ReleaseCrate();

        if (anchor != null)
        {
            HingeJoint2D joint = anchor.GetComponent<HingeJoint2D>();
            if (joint != null)
            {
                joint.enabled = false;
            }
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        DestroySelf();
    }
}