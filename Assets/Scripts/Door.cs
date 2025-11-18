using DG.Tweening;
using UnityEngine;
using static Lander;

public class Door : MonoBehaviour
{
    [SerializeField] private Key.KeyType keyType;
    [SerializeField] private GameObject leftDoor;
    [SerializeField] private GameObject rightDoor;

    private void Start()
    {
        Instance.OnKeyDeliver += Lander_OnKeyDeliver;
    }

    private void Lander_OnKeyDeliver(object sender, KeyDeliverEventArgs e)
    {
        if (e.DeliveredKeyType == keyType)
        {
            OpenDoor();
        }
    }

    private void OpenDoor()
    {
        leftDoor.transform.DORotate(new Vector3(0, 0, -90), 2f, RotateMode.LocalAxisAdd);
        rightDoor.transform.DORotate(new Vector3(0, 0, 90), 2f, RotateMode.LocalAxisAdd);
    }
}