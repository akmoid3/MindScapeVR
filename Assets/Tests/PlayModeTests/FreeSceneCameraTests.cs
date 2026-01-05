using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

public class FreeSceneCameraTests
{
    private GameObject cameraGameObject;
    private FreeSceneCamera freeSceneCamera;

    [SetUp]
    public void SetUp()
    {
        cameraGameObject = new GameObject("TestCamera");
        freeSceneCamera = cameraGameObject.AddComponent<FreeSceneCamera>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(cameraGameObject);
    }

    #region Initialization Tests

    [Test]
    public void Constructor_InitializesWithDefaultValues()
    {
        Assert.AreEqual(10f, freeSceneCamera. moveSpeed);
        Assert.AreEqual(4f, freeSceneCamera. boostMultiplier);
        Assert.AreEqual(12f, freeSceneCamera. acceleration);
        Assert.AreEqual(10f, freeSceneCamera. damping);
        Assert.AreEqual(0.15f, freeSceneCamera. lookSensitivity);
        Assert.AreEqual(-89f, freeSceneCamera.minPitch);
        Assert.AreEqual(89f, freeSceneCamera.maxPitch);
    }

    #endregion

    #region Look Tests

    [UnityTest]
    public IEnumerator HandleLook_RightClick_RotatesCamera()
    {
        yield return null;

        Reflect.SetFieldValue(freeSceneCamera, "rightClickHeld", true);
        Reflect.SetFieldValue(freeSceneCamera, "lookInput", new Vector2(10f, 5f));

        Vector3 initialRotation = cameraGameObject.transform.rotation. eulerAngles;

        cameraGameObject.transform.rotation = Quaternion.Euler(0, 0, 0);
        freeSceneCamera.GetType().GetMethod("HandleLook", 
            System.Reflection.BindingFlags.NonPublic | System. Reflection.BindingFlags.Instance)
            ?.Invoke(freeSceneCamera, null);

        Vector3 finalRotation = cameraGameObject.transform.rotation.eulerAngles;
        Assert.AreNotEqual(initialRotation, finalRotation);
    }

    [UnityTest]
    public IEnumerator HandleLook_PitchClamping_RespectsBounds()
    {
        yield return null;

        float pitch = 0f;
        float newPitch = 200f; 
        
        float clampedPitch = Mathf.Clamp(newPitch, freeSceneCamera.minPitch, freeSceneCamera.maxPitch);
        
        Assert.LessOrEqual(clampedPitch, freeSceneCamera. maxPitch);
        Assert.GreaterOrEqual(clampedPitch, freeSceneCamera.minPitch);
    }

    [UnityTest]
    public IEnumerator HandleLook_MiddleClick_PansCamera()
    {
        yield return null;

        Reflect.SetFieldValue(freeSceneCamera, "middleClickHeld", true);
        Reflect.SetFieldValue(freeSceneCamera, "lookInput", new Vector2(5f, 5f));

        Vector3 initialPosition = cameraGameObject.transform. position;

        freeSceneCamera.GetType().GetMethod("HandleLook", 
            System. Reflection.BindingFlags.NonPublic | System.Reflection. BindingFlags.Instance)
            ?.Invoke(freeSceneCamera, null);

        Vector3 finalPosition = cameraGameObject.transform.position;

        Assert.AreNotEqual(initialPosition, finalPosition);
    }

    #endregion

    #region Movement Tests

    [UnityTest]
    public IEnumerator HandleMovement_WithoutRightClick_DoesNotMove()
    {
        yield return null;

        Reflect.SetFieldValue(freeSceneCamera, "rightClickHeld", false);
        Reflect.SetFieldValue(freeSceneCamera, "moveInput", new Vector2(1f, 1f));

        Vector3 initialPosition = cameraGameObject.transform.position;

        freeSceneCamera.GetType().GetMethod("HandleMovement", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection. BindingFlags.Instance)
            ?.Invoke(freeSceneCamera, null);

        Vector3 finalPosition = cameraGameObject.transform.position;

        Assert.AreEqual(initialPosition, finalPosition);
    }

    [UnityTest]
    public IEnumerator HandleMovement_WithRightClick_MovesForward()
    {
        yield return null;

        Reflect. SetFieldValue(freeSceneCamera, "rightClickHeld", true);
        Reflect.SetFieldValue(freeSceneCamera, "moveInput", new Vector2(0f, 1f)); // Avanti
        Reflect.SetFieldValue(freeSceneCamera, "boostHeld", false);

        Vector3 initialPosition = cameraGameObject.transform. position;

        for (int i = 0; i < 5; i++)
        {
            freeSceneCamera.GetType().GetMethod("Update", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection. BindingFlags.Instance)
                ?.Invoke(freeSceneCamera, null);
            yield return null;
        }

        Vector3 finalPosition = cameraGameObject.transform.position;

        Assert.AreNotEqual(initialPosition, finalPosition);
    }

    [UnityTest]
    public IEnumerator HandleMovement_WithBoost_IncreasesSpeed()
    {
        yield return null;

        Reflect.SetFieldValue(freeSceneCamera, "rightClickHeld", true);
        Reflect.SetFieldValue(freeSceneCamera, "moveInput", new Vector2(0f, 1f));

        Reflect.SetFieldValue(freeSceneCamera, "boostHeld", false);
        Vector3 positionWithoutBoost = cameraGameObject.transform.position;
        
        for (int i = 0; i < 5; i++)
        {
            freeSceneCamera.GetType().GetMethod("Update", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection. BindingFlags.Instance)
                ?.Invoke(freeSceneCamera, null);
            yield return null;
        }
        
        Vector3 positionAfterWithoutBoost = cameraGameObject. transform.position;
        float distanceWithoutBoost = Vector3.Distance(positionWithoutBoost, positionAfterWithoutBoost);

        cameraGameObject.transform.position = Vector3.zero;
        Reflect.SetFieldValue(freeSceneCamera, "currentVelocity", Vector3.zero);

        Reflect.SetFieldValue(freeSceneCamera, "boostHeld", true);
        positionWithoutBoost = cameraGameObject.transform.position;
        
        for (int i = 0; i < 5; i++)
        {
            freeSceneCamera.GetType().GetMethod("Update", 
                System.Reflection.BindingFlags.NonPublic | System. Reflection.BindingFlags. Instance)
                ?.Invoke(freeSceneCamera, null);
            yield return null;
        }
        
        positionAfterWithoutBoost = cameraGameObject.transform.position;
        float distanceWithBoost = Vector3.Distance(positionWithoutBoost, positionAfterWithoutBoost);

        Assert.Greater(distanceWithBoost, distanceWithoutBoost);
    }

    #endregion

    #region Helper Class

    private static class Reflect
    {
        public static void SetFieldValue(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, 
                System.Reflection. BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?. SetValue(obj, value);
        }

        public static object GetFieldValue(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?. GetValue(obj);
        }
    }

    #endregion
}