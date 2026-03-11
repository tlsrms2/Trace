using UnityEngine;
using UnityEngine.EventSystems;

public class TargetButton : MonoBehaviour, IPointerExitHandler
{
    [SerializeField] private ButtonSwapController controller;

    public void OnPointerExit(PointerEventData eventData)
    {
        if (controller != null)
            controller.ShowMain();
    }
}