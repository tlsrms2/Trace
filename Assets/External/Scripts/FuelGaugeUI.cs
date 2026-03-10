using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class FuelGaugeController : MonoBehaviour
{
    [Header("Fuel Settings")]
    [SerializeField] private float maxFuel = 100f;
    [SerializeField] private float currentFuel = 100f;
    [SerializeField] private float drainPerSecond = 25f;   // 스페이스 누를 때 감소량
    [SerializeField] private float recoverPerSecond = 15f; // 스페이스 뗄 때 회복량

    private Slider fuelSlider;

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
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            currentFuel -= drainPerSecond * Time.deltaTime;
        }
        else
        {
            currentFuel += recoverPerSecond * Time.deltaTime;
        }

        currentFuel = Mathf.Clamp(currentFuel, 0f, maxFuel);
        fuelSlider.value = currentFuel;
    }
}