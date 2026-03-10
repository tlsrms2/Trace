using UnityEngine;

public class TimeFreezeUI : MonoBehaviour
{
    [Header("Border RectTransforms")]
    [SerializeField] private RectTransform topBorder;
    [SerializeField] private RectTransform bottomBorder;
    [SerializeField] private RectTransform leftBorder;
    [SerializeField] private RectTransform rightBorder;

    [Header("Settings")]
    [SerializeField] private float targetThickness = 20f; // 최대로 조여올 두께
    [SerializeField] private float moveSpeed = 500f;       // 조여오고 수축하는 속도

    private float currentThickness = 0f;

    private void Update()
    {
        // 스페이스 누르고 있으면 안쪽으로 조여오고, 떼면 다시 돌아감
        float target = Input.GetKey(KeyCode.Space) ? targetThickness : 0f;

        // 나중에 timeScale=0을 써도 UI는 잘 움직이게 unscaledDeltaTime 사용
        currentThickness = Mathf.MoveTowards(
            currentThickness,
            target,
            moveSpeed * Time.unscaledDeltaTime
        );

        ApplyBorder(currentThickness);
    }

    private void ApplyBorder(float thickness)
    {
        if (topBorder != null)
            topBorder.sizeDelta = new Vector2(0f, thickness);

        if (bottomBorder != null)
            bottomBorder.sizeDelta = new Vector2(0f, thickness);

        if (leftBorder != null)
            leftBorder.sizeDelta = new Vector2(thickness, 0f);

        if (rightBorder != null)
            rightBorder.sizeDelta = new Vector2(thickness, 0f);
    }
}