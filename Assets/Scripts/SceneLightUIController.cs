using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SceneLightUIController : MonoBehaviour
{
    [Header("Light")] public Light targetLight;

    [Header("UI Sliders")] public Slider intensitySlider;
    public Slider yRotSlider;
    public Slider xRotSlider;

    [Header("UI Texts (TMP)")] public TMP_Text intensityValueText;
    public TMP_Text yRotValueText;
    public TMP_Text xRotValueText;


    public float length = 2f;

    private LineRenderer lr;

    private void Awake()
    {
        lr = this.gameObject.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.widthMultiplier = 0.02f;

        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.yellow;
        lr.endColor = Color.yellow;
    }

    private void Update()
    {
        if (targetLight == null) return;

        if (StateManager.Instance.CurrentState != State.Editing)
        {
            if (lr.enabled) lr.enabled = false;
            return;
        }

        if (!lr.enabled) lr.enabled = true;

        Vector3 start = targetLight.transform.position;
        Vector3 end = start + targetLight.transform.forward * length;

        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }

    private void Start()
    {
        if (targetLight == null)
        {
            Debug.LogError("No light assigned");
            return;
        }

        if (intensitySlider != null)
        {
            intensitySlider.minValue = 0f;
            intensitySlider.maxValue = 5f;
            intensitySlider.value = targetLight.intensity;
            intensitySlider.onValueChanged.AddListener(OnIntensityChanged);
        }

        if (yRotSlider != null || xRotSlider != null)
        {
            Vector3 euler = targetLight.transform.rotation.eulerAngles;

            if (yRotSlider != null)
            {
                yRotSlider.minValue = 0f;
                yRotSlider.maxValue = 360f;
                yRotSlider.value = euler.y;
                yRotSlider.onValueChanged.AddListener(OnYRotChanged);
            }

            if (xRotSlider != null)
            {
                xRotSlider.minValue = -90f;
                xRotSlider.maxValue = 90f;
                // euler.x è 0 360 quindi convertito in -180 180
                float x = euler.x;
                if (x > 180f) x -= 360f;
                xRotSlider.value = Mathf.Clamp(x, -90f, 90f);
                xRotSlider.onValueChanged.AddListener(OnXRotChanged);
            }
        }

        if (intensityValueText != null)
            intensityValueText.text = intensitySlider.value.ToString("0.00");

        if (yRotValueText != null)
            yRotValueText.text = yRotSlider.value.ToString("0");

        if (xRotValueText != null)
            xRotValueText.text = xRotSlider.value.ToString("0");
    }

    private void OnDestroy()
    {
        if (intensitySlider != null)
            intensitySlider.onValueChanged.RemoveListener(OnIntensityChanged);
        if (yRotSlider != null)
            yRotSlider.onValueChanged.RemoveListener(OnYRotChanged);
        if (xRotSlider != null)
            xRotSlider.onValueChanged.RemoveListener(OnXRotChanged);
    }

    private void OnIntensityChanged(float value)
    {
        if (targetLight == null) return;
        targetLight.intensity = value;

        if (intensityValueText != null)
            intensityValueText.text = value.ToString("0.00");
    }

    private void OnYRotChanged(float value)
    {
        if (targetLight == null) return;

        Vector3 euler = targetLight.transform.rotation.eulerAngles;
        euler.y = value;
        targetLight.transform.rotation = Quaternion.Euler(euler);

        if (yRotValueText != null)
            yRotValueText.text = value.ToString("0");
    }


    private void OnXRotChanged(float value)
    {
        if (targetLight == null) return;

        Vector3 euler = targetLight.transform.rotation.eulerAngles;
        euler.x = value;
        targetLight.transform.rotation = Quaternion.Euler(euler);

        if (xRotValueText != null)
            xRotValueText.text = value.ToString("0");
    }
    
    public void UpdateUIFromLight()
    {
        if (targetLight == null) return;

        if (intensitySlider != null)
        {
            intensitySlider.SetValueWithoutNotify(targetLight.intensity);
            if (intensityValueText != null) 
                intensityValueText.text = targetLight.intensity.ToString("0.00");
        }

        Vector3 euler = targetLight.transform.rotation.eulerAngles;

        if (yRotSlider != null)
        {
            yRotSlider.SetValueWithoutNotify(euler.y);
            if (yRotValueText != null)
                yRotValueText.text = euler.y.ToString("0");
        }

        if (xRotSlider != null)
        {
            float x = euler.x;
            if (x > 180f) x -= 360f;
            float clampedX = Mathf.Clamp(x, -90f, 90f);
        
            xRotSlider.SetValueWithoutNotify(clampedX);
            if (xRotValueText != null)
                xRotValueText.text = clampedX.ToString("0");
        }
    }
}