using UnityEngine;

public class Level10CameraZoomOut : MonoBehaviour
{
    private void Start()
    {
        Lander.Instance.OnStateChanged += Lander_OnStateChanged;
    }

    private void Lander_OnStateChanged(object sender, Lander.OnStateChangedEventArgs e)
    {
        if (e.state == Lander.State.Normal)
        {
            CinemachineCameraZoom2D.Instance.SetTargetOrthographicSize(20f);
        }
    }
}
