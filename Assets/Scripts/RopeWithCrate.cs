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
        // Получаем CrateOnRope компонент, который уже должен быть прикреплен к crate
        CrateOnRope crateOnRope = crate.GetComponent<CrateOnRope>();
        if (crateOnRope == null)
        {
            Debug.LogError("CrateOnRope component not found on crate! Please attach it in the editor.");
            return;
        }

        // Подписываемся на события
        crateOnRope.OnCrateCollider += HandleCrateCollision;
        CrateOnRope.Instance.OnCrateDrop += CrateOnRope_OnCrateDrop;

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

            Lander.Instance.OnStateChanged += Lander_OnStateChanged;
        }

        SoundManager.Instance.RopeWithCrateSpawned();
        GameManager.Instance.RopeWithCrateSpawned();
    }

    private void Lander_OnStateChanged(object sender, Lander.OnStateChangedEventArgs e)
    {
        if (e.state == Lander.State.GameOver)
        {
            DestroySelf();
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

        // Отписываемся от событий
        if (CrateOnRope.Instance != null)
        {
            CrateOnRope.Instance.OnCrateCollider -= HandleCrateCollision;
            CrateOnRope.Instance.OnCrateDrop -= CrateOnRope_OnCrateDrop;
        }

        if (Lander.Instance != null)
        {
            Lander.Instance.OnStateChanged -= Lander_OnStateChanged;
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        DestroySelf();
    }
}