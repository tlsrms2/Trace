using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class FuelGaugeController : MonoBehaviour
{
    [Header("Fuel Settings")]
    [SerializeField] private float maxFuel = 100f;
    [SerializeField] private float currentFuel = 100f;
    [SerializeField] private float drainPerSecond = 25f;
    [SerializeField] private float recoverPerSecond = 15f;

    [Header("Shader / Material")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material pressedGlowMaterial;

    [Header("UI Text")]
    [SerializeField] private TMP_Text fuelValueText;

    private Slider fuelSlider;
    private bool wasPressing;

    public float CurrentFuel => currentFuel;
    public float MaxFuel => maxFuel;
    public bool HasFuel => currentFuel > 0f;

    private void Awake()
    {
        fuelSlider = GetComponent<Slider>();

        currentFuel = maxFuel;

        fuelSlider.minValue = 0f;
        fuelSlider.maxValue = maxFuel;
        fuelSlider.value = currentFuel;

        if (fillImage == null && fuelSlider.fillRect != null)
        {
            fillImage = fuelSlider.fillRect.GetComponent<Image>();
        }

        if (fillImage != null && normalMaterial == null)
        {
            normalMaterial = fillImage.material;
        }

        if (fillImage != null && normalMaterial != null)
        {
            fillImage.material = normalMaterial;
        }

        UpdateFuelText();
    }

    private void Update()
    {
        bool isPressing = Input.GetKey(KeyCode.Space);

        if (isPressing)
        {
            currentFuel -= drainPerSecond * Time.deltaTime;
        }
        else
        {
            currentFuel += recoverPerSecond * Time.deltaTime;
        }

        currentFuel = Mathf.Clamp(currentFuel, 0f, maxFuel);
        fuelSlider.value = currentFuel;

        UpdateFuelText();
        UpdateShaderState(isPressing);
    }

    private void UpdateFuelText()
    {
        if (fuelValueText == null)
        {
            return;
        }

        int displayValue = Mathf.RoundToInt(fuelSlider.value);
        fuelValueText.text = $"{displayValue}%";
    }

    private void UpdateShaderState(bool isPressing)
    {
        if (fillImage == null)
        {
            return;
        }

        if (isPressing != wasPressing)
        {
            if (isPressing)
            {
                if (pressedGlowMaterial != null)
                {
                    fillImage.material = pressedGlowMaterial;
                }
            }
            else
            {
                if (normalMaterial != null)
                {
                    fillImage.material = normalMaterial;
                }
            }

            wasPressing = isPressing;
        }
    }
}
