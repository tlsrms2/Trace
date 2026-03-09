using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        Vector2 inputDir = new Vector2(x, y).normalized;

        transform.Translate(inputDir * moveSpeed * Time.deltaTime);
    }
}
