using Unity.VisualScripting;
using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    [SerializeField] private float speed;

    private Transform target;
    
    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (Vector2.Distance(transform.position, target.position) > 0.1f)
        {
            transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
        }
    }
}
