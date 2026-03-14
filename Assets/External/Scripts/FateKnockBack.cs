using UnityEngine;

/// <summary>
/// 이 스크립트를 적(돌진형)이나 고정된 암초의 Collider2D(IsTrigger = false) 객체에 부착하십시오.
/// 플레이어 함선과 충돌 시 강제적인 넉백 물리력을 가해 운명 궤도에서 분리시킵니다.
/// </summary>
public class FateKnockback : MonoBehaviour
{
    [Header("Knockback Settings")]
    [Tooltip("충돌 시 플레이어를 밀어내는 힘의 크기")]
    [SerializeField] private float knockbackForce = 15f;
    [Tooltip("상단 방향으로 약간 더 띄울지 여부 (충돌감 강화)")]
    [SerializeField] private float upwardsModifier = 0f;
    
    [Header("Damage (Optional)")]
    [Tooltip("HP가 있다면 넉백과 동시에 데미지를 가합니다.")]
    [SerializeField] private int damageAmount = 0;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            
            if (playerRb != null)
            {
                // 충돌 방향 계산 (장애물 -> 플레이어 방향)
                Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
                
                // 정중앙 충돌로 인해 dir이 0이 되는 것을 방지
                if (knockbackDir == Vector2.zero) 
                {
                    knockbackDir = UnityEngine.Random.insideUnitCircle.normalized;
                }

                // 위쪽으로 약간 힘을 보태어 튕겨져 나가는 연출 강화 (선택 사항)
                knockbackDir.y += upwardsModifier;
                knockbackDir.Normalize();

                // 플레이어에게 Impulse(순간적인 충격량) 가하기
                playerRb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);

                // 데미지 처리 로직이 있다면 여기서 호출
                // if (damageAmount > 0) collision.gameObject.GetComponent<PlayerStatus>()?.TakeDamage(damageAmount);
                
                // 효과음 발생
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayPlayerDeath(); // 임시 효과음, 추후 피격용 사운드로 교체 권장
                }
            }
        }
    }
}