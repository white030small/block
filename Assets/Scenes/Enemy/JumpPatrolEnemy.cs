using UnityEngine;

/// <summary>
/// 簡單巡邏怪：走一下跳一下，不會掉出地圖，不會攻擊
/// 
/// 設定：
/// 1. 掛在有 Rigidbody2D + Collider2D 的怪物上
/// 2. Ground Layer 選地面的 Layer
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class JumpPatrolEnemy : MonoBehaviour
{
    [Header("=== 移動 ===")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float jumpForce = 8f;

    [Header("=== 節奏 ===")]
    [Tooltip("走幾秒後跳一次")]
    [SerializeField] private float walkTime = 1.5f;

    [Header("=== 偵測 ===")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float edgeCheckDist = 1.5f;
    [SerializeField] private float wallCheckDist = 0.5f;

    private Rigidbody2D rb;
    private int direction = 1;
    private float walkTimer;
    private bool isGrounded;
    private int groundContacts;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        walkTimer = walkTime;
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        foreach (ContactPoint2D c in col.contacts)
        {
            if (c.normal.y > 0.5f) { groundContacts++; break; }
        }
    }

    private void OnCollisionExit2D(Collision2D col) 
    { 
        groundContacts = Mathf.Max(0, groundContacts - 1); 
    }

    private void Update()
    {
        isGrounded = groundContacts > 0;

        if (!isGrounded) return;

        // 懸崖或牆壁 → 轉向
        if (CheckEdge() || CheckWall())
            direction *= -1;

        // 走路
        rb.linearVelocity = new Vector2(direction * walkSpeed, rb.linearVelocity.y);

        // 面朝方向
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * direction;
        transform.localScale = s;

        // 計時跳躍
        walkTimer -= Time.deltaTime;
        if (walkTimer <= 0f)
        {
            rb.linearVelocity = new Vector2(direction * walkSpeed, jumpForce);
            walkTimer = walkTime;
        }
    }

    // 前方沒地面 = 懸崖
    private bool CheckEdge()
    {
        Vector2 origin = (Vector2)transform.position 
            + Vector2.right * direction * 0.5f 
            + Vector2.down * 0.1f;
        return !Physics2D.Raycast(origin, Vector2.down, edgeCheckDist, groundLayer);
    }

    // 前方有牆
    private bool CheckWall()
    {
        Vector2 origin = (Vector2)transform.position;
        return Physics2D.Raycast(origin, Vector2.right * direction, wallCheckDist, groundLayer);
    }

    private void OnDrawGizmosSelected()
    {
        // 懸崖偵測
        Gizmos.color = Color.magenta;
        Vector3 edgeOrigin = transform.position + Vector3.right * direction * 0.5f + Vector3.down * 0.1f;
        Gizmos.DrawLine(edgeOrigin, edgeOrigin + Vector3.down * edgeCheckDist);

        // 牆壁偵測
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * direction * wallCheckDist);
    }
}