using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class RotatingCameraTests
{
    [UnityTest]
    public IEnumerator Update_RotatesTransform_ByRotationSpeedTimesDeltaTime()
    {
        var go = new GameObject("RotatingCamera");
        var rc = go.AddComponent<RotatingCamera>();

        rc.rotationSpeed = 90f;          // degrees per second
        rc.rotationAxis = Vector3.up;

        // rotazione iniziale
        var startRot = go.transform.rotation;

        // aspetta un frame così Update() gira una volta con deltaTime reale
        yield return null;

        var endRot = go.transform.rotation;

        // angolo effettivo ruotato (in gradi)
        float rotatedAngle = Quaternion.Angle(startRot, endRot);

        // ci aspettiamo una rotazione > 0 e circa rotationSpeed * deltaTime
        float expected = rc.rotationSpeed * Time.deltaTime;

        Assert.Greater(rotatedAngle, 0f);

        // tolleranza: deltaTime può variare un po' in PlayMode
        Assert.That(rotatedAngle, Is.EqualTo(expected).Within(0.5f));

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator Update_DoesNotRotate_WhenRotationSpeedIsZero()
    {
        var go = new GameObject("RotatingCamera");
        var rc = go.AddComponent<RotatingCamera>();

        rc.rotationSpeed = 0f;
        rc.rotationAxis = Vector3.up;

        var startRot = go.transform.rotation;

        yield return null;

        var endRot = go.transform.rotation;

        Assert.That(Quaternion.Angle(startRot, endRot), Is.EqualTo(0f).Within(0.0001f));

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator Update_UsesCustomAxis()
    {
        var go = new GameObject("RotatingCamera");
        var rc = go.AddComponent<RotatingCamera>();

        rc.rotationSpeed = 90f;
        rc.rotationAxis = Vector3.right;

        var startEuler = go.transform.rotation.eulerAngles;

        yield return null;

        var endEuler = go.transform.rotation.eulerAngles;

        // Dovrebbe cambiare prevalentemente l'asse X (non perfetto per euler wrap, ma utile come sanity check)
        float dx = Mathf.Abs(NormalizeAngle180(endEuler.x) - NormalizeAngle180(startEuler.x));
        float dy = Mathf.Abs(NormalizeAngle180(endEuler.y) - NormalizeAngle180(startEuler.y));
        float dz = Mathf.Abs(NormalizeAngle180(endEuler.z) - NormalizeAngle180(startEuler.z));

        Assert.Greater(dx, 0f);
        Assert.Less(dy, 1f);
        Assert.Less(dz, 1f);

        Object.Destroy(go);
    }

    private static float NormalizeAngle180(float angle0to360)
    {
        var a = angle0to360;
        if (a > 180f) a -= 360f;
        return a;
    }
}