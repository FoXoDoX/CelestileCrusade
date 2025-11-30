using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class CrateLandingPadVisuals : MonoBehaviour
{
    [SerializeField] private Transform landedCrates;
    [SerializeField] private ParticleSystem deliveryParticle;

    private List<GameObject> processedCrates = new List<GameObject>();

    private void Start()
    {
        if (landedCrates != null)
        {
            foreach (Transform child in landedCrates)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    processedCrates.Add(child.gameObject);
                }
            }
        }
    }

    private void Update()
    {
        if (landedCrates == null) return;

        foreach (Transform child in landedCrates)
        {
            GameObject crate = child.gameObject;
            if (crate.activeInHierarchy && !processedCrates.Contains(crate))
            {
                ProcessNewCrate(crate);
                processedCrates.Add(crate);
            }
        }
    }

    private void ProcessNewCrate(GameObject crate)
    {
        crate.transform.localScale = Vector3.zero;
        crate.transform.DOScale(Vector3.one, 1f)
            .SetEase(Ease.OutBack)
            .OnStart(() =>
            {
                if (deliveryParticle != null)
                {
                    ParticleSystem particle = Instantiate(
                        deliveryParticle,
                        crate.transform.position,
                        Quaternion.identity
                    );
                    particle.Play();

                    Destroy(particle.gameObject, particle.main.duration);
                }
            })
            .SetLink(crate);
    }
}