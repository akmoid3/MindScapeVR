using UnityEngine;

public class RotatingCamera : MonoBehaviour
{
    [Header("Settings")]
    public float rotationSpeed = 5f;
    public Vector3 rotationAxis = Vector3.up;

    void Update()
    {
        float angle = rotationSpeed * Time.deltaTime;

        transform.Rotate(rotationAxis * angle);
    }
}