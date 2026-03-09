using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnInterval;
    [SerializeField] private float xOffset = 15.0f;
    [SerializeField] private float yOffset = 10.0f;
    private float timer;
    private Transform _playerTransform;

    void Start()
    {
        timer = 0f;
        _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnEnemy();
            timer = 0f;
        }
    }

    void SpawnEnemy()
    {
        int flag = Random.Range(0, 2);
        Vector3 playerPosition = _playerTransform.position;
        Vector3 randomPosition = Vector3.zero;

        float x = 0.0f;
        float y = 0.0f;
        if (flag == 0)
        {
            x = playerPosition.x;
            x += Random.Range(0, 2) == 0 ? -xOffset : xOffset;
            y = Random.Range(playerPosition.y - yOffset, playerPosition.y + yOffset);
        }
        else
        {
            x = Random.Range(playerPosition.x - xOffset, playerPosition.x + xOffset);
            y = playerPosition.y;
            y += Random.Range(0, 2) == 0 ? -yOffset : yOffset;
        }
        randomPosition = new Vector3(x, y, 0);
        Instantiate(enemyPrefab, randomPosition, Quaternion.identity);
    }

    private void OnDrawGizmos()
    {
        // 에디터에서 플레이어 변수가 할당되지 않았을 때 오류 방지
        if (_playerTransform == null)
        {
            // 실행 중이 아닐 때는 수동으로 플레이어를 찾음 (에디터 편의용)
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _playerTransform = player.transform;
            else return;
        }

        float xOffset = this.xOffset;
        float yOffset = this.yOffset;
        Vector3 center = _playerTransform.position;

        // 기즈모 색상 설정 (원하는 색으로 변경 가능)
        Gizmos.color = Color.red;

        // 적이 생성되는 직사각형 테두리를 선으로 그림
        // 중심점, 크기(가로=xOffset*2, 세로=yOffset*2)
        Vector3 size = new Vector3(xOffset * 2, yOffset * 2, 0);
        Gizmos.DrawWireCube(center, size);

        // 생성 지점(네 변)임을 강조하기 위해 구체 표시 (선택 사항)
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(new Vector3(center.x + xOffset, center.y, 0), 0.5f); // 우측
        Gizmos.DrawSphere(new Vector3(center.x - xOffset, center.y, 0), 0.5f); // 좌측
        Gizmos.DrawSphere(new Vector3(center.x, center.y + yOffset, 0), 0.5f); // 상단
        Gizmos.DrawSphere(new Vector3(center.x, center.y - yOffset, 0), 0.5f); // 하단
    }
}
