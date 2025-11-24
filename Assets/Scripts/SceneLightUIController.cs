using UnityEngine;
using UnityEngine.UI;

public class SceneLightUIController : MonoBehaviour
{
    [Header("Light")]
    public Light targetLight;            

    [Header("UI Sliders")]
    public Slider intensitySlider;
    public Slider yRotSlider;          
    public Slider xRotSlider;       

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
    }

    private void OnYRotChanged(float value)
    {
        if (targetLight == null) return;

        Vector3 euler = targetLight.transform.rotation.eulerAngles;
        euler.y = value;
        targetLight.transform.rotation = Quaternion.Euler(euler);
    }

    private void OnXRotChanged(float value)
    {
        if (targetLight == null) return;

        Vector3 euler = targetLight.transform.rotation.eulerAngles;
        euler.x = value;
        targetLight.transform.rotation = Quaternion.Euler(euler);
    }
}