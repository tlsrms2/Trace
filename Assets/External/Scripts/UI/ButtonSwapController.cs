using UnityEngine;

public class ButtonSwapController : MonoBehaviour
{
    [SerializeField] private GameObject mainButton;
    [SerializeField] private GameObject targetButton;

    private void Awake()
    {
        ShowMain();
    }

    public void ShowTarget()
    {
        if (mainButton != null)
            mainButton.SetActive(false);

        if (targetButton != null)
            targetButton.SetActive(true);
    }

    public void ShowMain()
    {
        if (mainButton != null)
            mainButton.SetActive(true);

        if (targetButton != null)
            targetButton.SetActive(false);
    }
}