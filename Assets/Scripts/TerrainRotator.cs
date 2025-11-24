using UnityEngine;

public class TerrainRotator : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;

    private Vector3 rotationCenter;

    void Start()
    {
        CalculateRotationCenter();
        Lander.Instance.OnLanded += Lander_OnLanded;
    }

    private void Lander_OnLanded(object sender, Lander.OnLandedEventArgs e)
    {
        if (e.landingType == Lander.LandingType.Success) 
        { 
            Lander.Instance.transform.SetParent(transform);
        }
    }

    void Update()
    {
        transform.RotateAround(rotationCenter, rotationAxis, rotationSpeed * Time.deltaTime);
    }

    private void CalculateRotationCenter()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            rotationCenter = renderer.bounds.center;
            return;
        }
    }
}