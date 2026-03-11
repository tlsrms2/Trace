using UnityEngine;
using UnityEngine.EventSystems;

public class MainButton : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private ButtonSwapController controller;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (controller != null)
            controller.ShowTarget();
    }
}