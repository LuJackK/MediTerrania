using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SeaTemperatureController : MonoBehaviour
{
    [Header("Temperature Settings")]
    public float seaTemperature = 18f;
    public float minTemperature = 8f;
    public float maxTemperature = 30f;
    public float step = 1f;

    [Header("UI")]
    public TMP_Text temperatureText;
    public Image thermometerFill;

    public void IncreaseTemperature()
    {
        seaTemperature = Mathf.Clamp(seaTemperature + step, minTemperature, maxTemperature);
        UpdateUI();
    }

    public void DecreaseTemperature()
    {
        seaTemperature = Mathf.Clamp(seaTemperature - step, minTemperature, maxTemperature);
        UpdateUI();
    }

    private void Start()
    {
        ConfigureThermometerFill();
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (temperatureText != null)
        {
            temperatureText.text = seaTemperature.ToString("0") + "\u00B0C";
        }

        if (thermometerFill != null)
        {
            float normalizedTemp = Mathf.InverseLerp(minTemperature, maxTemperature, seaTemperature);
            thermometerFill.fillAmount = normalizedTemp;
        }

        Debug.Log("Sea temperature: " + seaTemperature + "\u00B0C");
    }

    private void ConfigureThermometerFill()
    {
        if (thermometerFill == null)
        {
            return;
        }

        thermometerFill.type = Image.Type.Filled;
        thermometerFill.fillMethod = Image.FillMethod.Vertical;
        thermometerFill.fillOrigin = (int)Image.OriginVertical.Bottom;
    }

    private void OnValidate()
    {
        seaTemperature = Mathf.Clamp(seaTemperature, minTemperature, maxTemperature);
        ConfigureThermometerFill();
    }
}
