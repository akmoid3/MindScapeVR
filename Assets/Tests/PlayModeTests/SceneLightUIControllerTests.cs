using System.Collections;
using System.Globalization;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class SceneLightUIControllerTests
{
    private GameObject _stateManagerGO;

    [UnitySetUp]
    public IEnumerator UnitySetUp()
    {
        if (StateManager.Instance == null)
        {
            _stateManagerGO = new GameObject("StateManager");
            _stateManagerGO.AddComponent<StateManager>();
        }

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator UnityTearDown()
    {
        if (_stateManagerGO != null)
            Object.Destroy(_stateManagerGO);

        yield return null;
    }

    private static Slider CreateSlider(string name, out GameObject go)
    {
        go = new GameObject(name);
        if (go.GetComponent<RectTransform>() == null)
            go.AddComponent<RectTransform>();

        return go.AddComponent<Slider>();
    }

    private static TMP_Text CreateTMPText(string name, out GameObject go)
    {
        go = new GameObject(name);
        if (go.GetComponent<RectTransform>() == null)
            go.AddComponent<RectTransform>();

        return go.AddComponent<TextMeshProUGUI>();
    }

    [UnityTest]
    public IEnumerator Start_InitializesSlidersAndTexts_FromLight()
    {
        var lightGO = new GameObject("DirLight");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 2.5f;
        lightGO.transform.rotation = Quaternion.Euler(30f, 120f, 0f);

        var controllerGO = new GameObject("Controller");
        var controller = controllerGO.AddComponent<SceneLightUIController>();
        controller.targetLight = light;

        controller.intensitySlider = CreateSlider("IntensitySlider", out var intensitySliderGO);
        controller.yRotSlider = CreateSlider("YRotSlider", out var yRotSliderGO);
        controller.xRotSlider = CreateSlider("XRotSlider", out var xRotSliderGO);

        controller.intensityValueText = CreateTMPText("IntensityText", out var intensityTextGO);
        controller.yRotValueText = CreateTMPText("YRotText", out var yRotTextGO);
        controller.xRotValueText = CreateTMPText("XRotText", out var xRotTextGO);

        yield return null; // Start()

        Assert.AreEqual(0f, controller.intensitySlider.minValue);
        Assert.AreEqual(5f, controller.intensitySlider.maxValue);
        Assert.AreEqual(2.5f, controller.intensitySlider.value, 0.0001f);
        Assert.AreEqual(2.5f.ToString("0.00", CultureInfo.CurrentCulture), controller.intensityValueText.text);

        Assert.AreEqual(0f, controller.yRotSlider.minValue);
        Assert.AreEqual(360f, controller.yRotSlider.maxValue);
        Assert.AreEqual(120f, controller.yRotSlider.value, 0.0001f);
        Assert.AreEqual(120f.ToString("0", CultureInfo.CurrentCulture), controller.yRotValueText.text);

        Assert.AreEqual(-90f, controller.xRotSlider.minValue);
        Assert.AreEqual(90f, controller.xRotSlider.maxValue);
        Assert.AreEqual(30f, controller.xRotSlider.value, 0.0001f);
        Assert.AreEqual(30f.ToString("0", CultureInfo.CurrentCulture), controller.xRotValueText.text);

        Object.Destroy(controllerGO);
        Object.Destroy(lightGO);
        Object.Destroy(intensitySliderGO);
        Object.Destroy(yRotSliderGO);
        Object.Destroy(xRotSliderGO);
        Object.Destroy(intensityTextGO);
        Object.Destroy(yRotTextGO);
        Object.Destroy(xRotTextGO);
    }

    [UnityTest]
    public IEnumerator SliderChanges_UpdateLight_AndTexts()
    {
        var lightGO = new GameObject("DirLight");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        lightGO.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        var controllerGO = new GameObject("Controller");
        var controller = controllerGO.AddComponent<SceneLightUIController>();
        controller.targetLight = light;

        controller.intensitySlider = CreateSlider("IntensitySlider", out var intensitySliderGO);
        controller.yRotSlider = CreateSlider("YRotSlider", out var yRotSliderGO);
        controller.xRotSlider = CreateSlider("XRotSlider", out var xRotSliderGO);

        controller.intensityValueText = CreateTMPText("IntensityText", out var intensityTextGO);
        controller.yRotValueText = CreateTMPText("YRotText", out var yRotTextGO);
        controller.xRotValueText = CreateTMPText("XRotText", out var xRotTextGO);

        yield return null; // Start()

        controller.intensitySlider.value = 3.25f; // trigger listener
        Assert.AreEqual(3.25f, light.intensity, 0.0001f);
        Assert.AreEqual(3.25f.ToString("0.00", CultureInfo.CurrentCulture), controller.intensityValueText.text);

        controller.yRotSlider.value = 200f;
        Assert.AreEqual(200f, lightGO.transform.rotation.eulerAngles.y, 0.0001f);
        Assert.AreEqual(200f.ToString("0", CultureInfo.CurrentCulture), controller.yRotValueText.text);

        controller.xRotSlider.value = -45f;
        // eulerAngles.x returns 0..360 => normalize for comparison
        Assert.AreEqual(-45f, NormalizeAngle180(lightGO.transform.rotation.eulerAngles.x), 0.0001f);
        Assert.AreEqual((-45f).ToString("0", CultureInfo.CurrentCulture), controller.xRotValueText.text);

        Object.Destroy(controllerGO);
        Object.Destroy(lightGO);
        Object.Destroy(intensitySliderGO);
        Object.Destroy(yRotSliderGO);
        Object.Destroy(xRotSliderGO);
        Object.Destroy(intensityTextGO);
        Object.Destroy(yRotTextGO);
        Object.Destroy(xRotTextGO);
    }

    [UnityTest]
    public IEnumerator Update_TogglesLineRenderer_BasedOnState()
    {
        var lightGO = new GameObject("DirLight");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;

        var controllerGO = new GameObject("Controller");
        var controller = controllerGO.AddComponent<SceneLightUIController>();
        controller.targetLight = light;

        yield return null; // Awake/Start

        var lr = controllerGO.GetComponent<LineRenderer>();
        Assert.IsNotNull(lr, "LineRenderer should be added in Awake.");

        StateManager.Instance.UpdateState(State.MainMenu);
        yield return null;
        Assert.IsFalse(lr.enabled);

        StateManager.Instance.UpdateState(State.Editing);
        yield return null;
        Assert.IsTrue(lr.enabled);

        Object.Destroy(controllerGO);
        Object.Destroy(lightGO);
    }

    [UnityTest]
    public IEnumerator UpdateUIFromLight_SyncsSlidersAndTexts_WithoutInvokingCallbacks()
    {
        var lightGO = new GameObject("DirLight");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;

        var controllerGO = new GameObject("Controller");
        var controller = controllerGO.AddComponent<SceneLightUIController>();
        controller.targetLight = light;

        controller.intensitySlider = CreateSlider("IntensitySlider", out var intensitySliderGO);
        controller.yRotSlider = CreateSlider("YRotSlider", out var yRotSliderGO);
        controller.xRotSlider = CreateSlider("XRotSlider", out var xRotSliderGO);

        controller.intensityValueText = CreateTMPText("IntensityText", out var intensityTextGO);
        controller.yRotValueText = CreateTMPText("YRotText", out var yRotTextGO);
        controller.xRotValueText = CreateTMPText("XRotText", out var xRotTextGO);

        yield return null; // Start()

        int intensityCalls = 0;
        int yCalls = 0;
        int xCalls = 0;

        controller.intensitySlider.onValueChanged.AddListener(_ => intensityCalls++);
        controller.yRotSlider.onValueChanged.AddListener(_ => yCalls++);
        controller.xRotSlider.onValueChanged.AddListener(_ => xCalls++);

        light.intensity = 4f;
        lightGO.transform.rotation = Quaternion.Euler(10f, 55f, 0f);

        controller.UpdateUIFromLight();

        Assert.AreEqual(4f, controller.intensitySlider.value, 0.0001f);
        Assert.AreEqual(4f.ToString("0.00", CultureInfo.CurrentCulture), controller.intensityValueText.text);

        Assert.AreEqual(55f, controller.yRotSlider.value, 0.0001f);
        Assert.AreEqual(55f.ToString("0", CultureInfo.CurrentCulture), controller.yRotValueText.text);

        Assert.AreEqual(10f, controller.xRotSlider.value, 0.0001f);
        Assert.AreEqual(10f.ToString("0", CultureInfo.CurrentCulture), controller.xRotValueText.text);

        Assert.AreEqual(0, intensityCalls);
        Assert.AreEqual(0, yCalls);
        Assert.AreEqual(0, xCalls);

        Object.Destroy(controllerGO);
        Object.Destroy(lightGO);
        Object.Destroy(intensitySliderGO);
        Object.Destroy(yRotSliderGO);
        Object.Destroy(xRotSliderGO);
        Object.Destroy(intensityTextGO);
        Object.Destroy(yRotTextGO);
        Object.Destroy(xRotTextGO);
    }
    
    [UnityTest]
    public IEnumerator Start_WhenTargetLightIsNull_LogsErrorAndReturns()
    {
        LogAssert.Expect(LogType.Error, "No light assigned");

        var controllerGO = new GameObject("Controller");
        var controller = controllerGO.AddComponent<SceneLightUIController>();
        controller.targetLight = null;

        yield return null;

        Object.Destroy(controllerGO);
    }

    private static float NormalizeAngle180(float angle0to360)
    {
        var a = angle0to360;
        if (a > 180f) a -= 360f;
        return a;
    }
}