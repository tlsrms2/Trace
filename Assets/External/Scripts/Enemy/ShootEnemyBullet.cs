using UnityEngine;

public class ShootEnemyBullet : MonoBehaviour
{
    private Rigidbody2D myRigidbody;
    private Vector2 myVec;

    private void Awake()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
        {
            myRigidbody.linearVelocity = Vector2.zero;
        }
        else
        {
            myRigidbody.linearVelocity = myVec;
            float angle = Mathf.Atan2(myVec.y, myVec.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }
    }

    public void Shoot(Vector2 vec)
    {
        myVec = vec;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}
