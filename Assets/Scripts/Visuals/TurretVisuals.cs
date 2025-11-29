using UnityEngine;
using DG.Tweening;

public class TurretVisuals : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform gunTransform;
    [SerializeField] private ParticleSystem turretShootSmokeParticleSystem;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform gunPivot;

    [Header("Animation Settings")]
    [SerializeField] private float recoilDuration = 0.05f;
    [SerializeField] private float returnDuration = 0.4f;
    [SerializeField] private float recoilDistance = 1.5f;
    [SerializeField] private float shakeIntensity = 0.5f;
    [SerializeField] private int shakeVibrato = 15;

    private Vector3 originalGunPosition;
    private bool isAnimating = false;

    private void Awake()
    {
        if (gunTransform == null)
        {
            Debug.LogError("[TurretVisuals] Gun Transform is not assigned!");
            return;
        }

        originalGunPosition = gunTransform.localPosition;
    }

    private void Start()
    {
        Turret.OnTurretShoot += Turret_OnTurretShoot;
    }

    private void Turret_OnTurretShoot(object sender, System.EventArgs e)
    {
        Turret shootingTurret = sender as Turret;
        if (shootingTurret != null && shootingTurret == GetComponent<Turret>())
        {
            PlayShootAnimation();
            PlaySmokeEffect();
        }
    }

    private void PlayShootAnimation()
    {
        if (gunTransform == null || isAnimating) return;

        isAnimating = true;

        Vector3 recoilDirection = -gunTransform.up;
        Vector3 recoilPosition = originalGunPosition + recoilDirection * recoilDistance;

        Sequence shootSequence = DOTween.Sequence();

        shootSequence.Append(gunTransform.DOLocalMove(recoilPosition, recoilDuration)
            .SetEase(Ease.OutCubic));

        shootSequence.Join(gunTransform.DOShakePosition(recoilDuration, shakeIntensity, shakeVibrato, 90f, false, false));

        shootSequence.Append(gunTransform.DOLocalMove(originalGunPosition, returnDuration)
            .SetEase(Ease.Linear));

        shootSequence.OnComplete(() =>
        {
            isAnimating = false;
            if (gunTransform != null)
                gunTransform.localPosition = originalGunPosition;
        });

        shootSequence.OnKill(() =>
        {
            isAnimating = false;
            if (gunTransform != null)
                gunTransform.localPosition = originalGunPosition;
        });

        shootSequence.SetAutoKill(true);
    }

    private void PlaySmokeEffect()
    {
        if (turretShootSmokeParticleSystem == null || firePoint == null) return;

        try
        {
            ParticleSystem smokeInstance = Instantiate(
                turretShootSmokeParticleSystem,
                firePoint.position,
                Quaternion.Euler(-90f, 0f, 0f)
            );

            if (gunPivot != null)
            {
                var shape = smokeInstance.shape;
                shape.rotation = new Vector3(0f, (360 - gunPivot.localEulerAngles.z) + (360 - transform.eulerAngles.z), 0f);
            }

            smokeInstance.Play();

            float particleDuration = smokeInstance.main.duration + smokeInstance.main.startLifetime.constantMax;
            Destroy(smokeInstance.gameObject, particleDuration);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TurretVisuals] Error playing smoke effect: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        Turret.OnTurretShoot -= Turret_OnTurretShoot;

        if (gunTransform != null)
        {
            gunTransform.DOKill();
            gunTransform.localPosition = originalGunPosition;
        }
    }
}