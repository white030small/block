using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(PlayerHealth))]
public class mainchar : MonoBehaviour
{
    [Header("=== 移動與翻滾設定 ===")]
    [SerializeField] private float cubeSize = 1f;
    [SerializeField] private float rollDuration = 0.2f;

    [Header("=== 跳躍與下壓設定 ===")]
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float groundPoundSpeed = 25f;
    [SerializeField] private float jumpHorizontalForce = 8f;

    [Header("=== 衝刺設定 ===")]
    [SerializeField] private float dashSpeed = 30f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("=== 戰鬥設定 ===")]
    [SerializeField] private float damageRadius = 1.5f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("=== 下壓掉肉塊 ===")]
    [SerializeField] private int spawnCubeCount = 2;
    [Tooltip("肉塊從多高掉下來")]
    [SerializeField] private float cubeDropHeight = 8f;
    [Tooltip("肉塊左右散開的範圍")]
    [SerializeField] private float cubeSpreadRange = 3f;

    // 元件
    private Rigidbody2D rb;
    private PlayerHealth playerHealth;

    // 狀態
    private bool isRolling = false;
    private bool isGroundPounding = false;
    private bool isDashing = false;
    private float dashCooldownTimer = 0f;
    private float lastMoveDirection = 1f;

    // 地面偵測
    private int contactCount = 0;
    private bool isGrounded => contactCount > 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void OnCollisionEnter2D(Collision2D collision) { contactCount++; }
    private void OnCollisionExit2D(Collision2D collision) { contactCount--; }

    private void Update()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        if (horizontalInput != 0)
            lastMoveDirection = horizontalInput > 0 ? 1f : -1f;

        // 衝刺
        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0
            && !isRolling && !isGroundPounding && !isDashing)
        {
            StartCoroutine(Dash());
        }
        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;

        // 翻滾（地面 + 有輸入）
        if (!isRolling && !isGroundPounding && !isDashing && isGrounded && horizontalInput != 0)
        {
            StartCoroutine(RollCube(horizontalInput > 0 ? 1 : -1));
        }

        // 跳躍
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isRolling && !isDashing)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            Vector2 jumpVector = new Vector2(lastMoveDirection * jumpHorizontalForce, jumpForce);
            rb.AddForce(jumpVector, ForceMode2D.Impulse);
        }

        // 下壓（空中 + 滑鼠左鍵）
        if (Input.GetMouseButtonDown(0) && !isGrounded && !isGroundPounding && !isDashing)
        {
            // 檢查血量：1 格血不能下壓
            if (playerHealth != null && !playerHealth.TryGroundPound())
            {
                // TryGroundPound 已經顯示警告了，什麼都不做
            }
            else
            {
                StartCoroutine(GroundPound());
            }
        }
    }

    // ============================================================
    //  衝刺
    // ============================================================
    private IEnumerator Dash()
    {
        isDashing = true;
        dashCooldownTimer = dashCooldown;

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0;
        rb.linearVelocity = new Vector2(lastMoveDirection * dashSpeed, 0);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originalGravity;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        isDashing = false;
    }

    // ============================================================
    //  翻滾
    // ============================================================
    private IEnumerator RollCube(int direction)
    {
        isRolling = true;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;

        float elapsed = 0f;
        float half = cubeSize / 2f;
        Vector3 pivot = transform.position + new Vector3(direction * half, -half, 0f);

        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;
            transform.RotateAround(pivot, Vector3.forward,
                -90 * direction * (Time.deltaTime / rollDuration));
            yield return null;
        }

        transform.rotation = Quaternion.Euler(0, 0,
            Mathf.Round(transform.eulerAngles.z / 90f) * 90f);
        rb.position = transform.position;
        rb.bodyType = RigidbodyType2D.Dynamic;

        isRolling = false;
    }

    // ============================================================
    //  下壓
    // ============================================================
    private IEnumerator GroundPound()
    {
        isGroundPounding = true;

        // ★ 下壓瞬間從身上噴出肉塊
        SpawnMeatCubes();

        // 短暫滯空
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0;
        yield return new WaitForSeconds(0.1f);

        // 向下衝
        rb.gravityScale = 4;
        rb.linearVelocity = new Vector2(0, -groundPoundSpeed);

        // 下墜中偵測敵人
        bool hasDamaged = false;
        float timeout = 5f;

        while (!isGrounded && timeout > 0f)
        {
            timeout -= Time.deltaTime;

            if (!hasDamaged)
            {
                Collider2D hit = Physics2D.OverlapCircle(
                    transform.position + Vector3.down * 0.5f,
                    damageRadius, enemyLayer);

                if (hit != null)
                {
                    Debug.Log($"[mainchar] 下壓命中: {hit.gameObject.name}");
                    hasDamaged = true;
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.8f);
                }
            }
            yield return null;
        }

        isGroundPounding = false;
    }

    // ============================================================
    //  噴肉塊
    // ============================================================
    private void SpawnMeatCubes()
    {
        Debug.Log($"[mainchar] 從天上掉下 {spawnCubeCount} 個肉塊！");

        for (int i = 0; i < spawnCubeCount; i++)
        {
            // 在主角上方隨機位置生成
            float randomX = Random.Range(-cubeSpreadRange, cubeSpreadRange);
            Vector3 spawnPos = transform.position
                + Vector3.up * cubeDropHeight
                + Vector3.right * randomX;

            GameObject cube = new GameObject($"MeatCube_{i}");
            cube.transform.position = spawnPos;

            Rigidbody2D cubeRb = cube.AddComponent<Rigidbody2D>();
            cubeRb.gravityScale = 3f;
            cubeRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            cube.AddComponent<HealthCube>();
            // 不加力，讓重力自然掉下來
        }
    }

    // ============================================================
    //  Gizmos
    // ============================================================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.down * 0.5f, damageRadius);
    }
}