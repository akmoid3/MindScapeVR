using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class LookAtCameraTests
{
    [UnityTest]
    public IEnumerator Update_WhenCamIsNull_DoesNotChangeRotation()
    {
        var go = new GameObject("LookAtTarget");
        var comp = go.AddComponent<LookAtCamera>();

        go.transform.rotation = Quaternion.Euler(10f, 20f, 30f);
        var startRot = go.transform.rotation;

        yield return null; // Update()

        Assert.That(Quaternion.Angle(startRot, go.transform.rotation), Is.EqualTo(0f).Within(0.0001f));

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator Cam_SetsCameraReference()
    {
        var go = new GameObject("LookAtTarget");
        var comp = go.AddComponent<LookAtCamera>();

        var camGO = new GameObject("Cam");
        comp.Cam(camGO);

        // Aspetta un frame e verifica indirettamente: Update deve ruotare verso cam
        go.transform.position = Vector3.zero;
        camGO.transform.position = new Vector3(0f, 0f, 10f); // davanti in +Z

        yield return null; // Update() => LookAt(+Z)

        var forward = go.transform.forward;
        Assert.Greater(Vector3.Dot(forward, Vector3.forward), 0.999f);

        Object.Destroy(go);
        Object.Destroy(camGO);
    }

    [UnityTest]
    public IEnumerator Update_LooksAtCameraTransform()
    {
        var go = new GameObject("LookAtTarget");
        var comp = go.AddComponent<LookAtCamera>();

        var camGO = new GameObject("Cam");
        comp.Cam(camGO);

        go.transform.position = Vector3.zero;
        camGO.transform.position = new Vector3(10f, 0f, 0f); // alla destra (+X)

        // Set rotazione iniziale diversa
        go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        yield return null; // Update()

        // Dopo LookAt, forward dovrebbe puntare verso +X
        var expectedDir = (camGO.transform.position - go.transform.position).normalized;
        Assert.Greater(Vector3.Dot(go.transform.forward, expectedDir), 0.999f);

        Object.Destroy(go);
        Object.Destroy(camGO);
    }
}