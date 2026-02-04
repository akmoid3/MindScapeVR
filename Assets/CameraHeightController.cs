using UnityEngine;

public class CameraHeightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraOffset;

    [Header("Height Settings")]
    [SerializeField] private float step = 0.02f;


    public void IncreaseHeight()
    {
        if (cameraOffset == null) return;

        Vector3 pos = cameraOffset.localPosition;
        pos.y += step;
        cameraOffset.localPosition = pos;

        Debug.Log($"Camera height set to {pos.y}");
    }

    public void DecreaseHeight()
    {
        if (cameraOffset == null) return;

        Vector3 pos = cameraOffset.localPosition;
        pos.y -= step;
        cameraOffset.localPosition = pos;

        Debug.Log($"Camera height set to {pos.y}");
    }
}