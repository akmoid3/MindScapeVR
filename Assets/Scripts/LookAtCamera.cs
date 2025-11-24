using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    [SerializeField] private GameObject cam;


    void Update()
    {
        if (cam == null) return;

        transform.LookAt(cam.transform);
    }

    public void Cam(GameObject camera)
    {
        cam = camera;
    }
}
