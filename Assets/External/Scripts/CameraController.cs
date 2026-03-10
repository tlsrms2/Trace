using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;   // 따라갈 플레이어
    [SerializeField] private float smoothSpeed = 5f; // 부드러운 이동 속도
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10); // 카메라 위치 보정

    private void LateUpdate()
    {
        if (target == null) return;

        // 목표 위치 = 플레이어 위치 + offset
        Vector3 desiredPosition = target.position + offset;

        // 부드럽게 이동 (Lerp 사용)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        transform.position = smoothedPosition;
    }
}
