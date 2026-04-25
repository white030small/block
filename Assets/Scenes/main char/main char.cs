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

    [Header("=== 攻擊噴方塊設定 ===")]
    [Tooltip("命中敵人時噴出的方塊 Prefab（留空會自動建立）")]
    [SerializeField] private GameObject healthCubePrefab;
    [Tooltip("噴出方塊的數量")]
    [SerializeField] private int spawnCubeCount = 2;
    [Tooltip("噴出的力道")]
    [SerializeField] private float cubeSpawnForce = 8f;

    private Rigidbody2D rb;
    private PlayerHealth playerHealth;
    private bool isRolling = false;
    private bool isGroundPounding = false;
    private bool isDashing = false;

    private float dashCooldownTimer = 0f;
    private float lastMoveDirection = 1f;

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
        // 1. 記錄最後移動方向
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        if (horizontalInput != 0) lastMoveDirection = horizontalInput > 0 ? 1f : -1f;

        // 2. 衝刺輸入 (Shift)
        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0 && !isRolling && !isGroundPounding && !isDashing)
        {
            StartCoroutine(Dash());
        }
        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;

        // 3. 翻滾 (限地面，靜止時不翻滾)
        if (!isRolling && !isGroundPounding && !isDashing && isGrounded && horizontalInput != 0)
        {
            StartCoroutine(RollCube(horizontalInput > 0 ? 1 : -1));
        }

        // 4. 跳躍
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isRolling && !isDashing)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            Vector2 jumpVector = new Vector2(lastMoveDirection * jumpHorizontalForce, jumpForce);
            rb.AddForce(jumpVector, ForceMode2D.Impulse);
        }

        // 5. 下壓
        if (Input.GetMouseButtonDown(0) && !isGrounded && !isGroundPounding && !isDashing)
        {
            StartCoroutine(GroundPound());
        }
    }

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
            transform.RotateAround(pivot, Vector3.forward, -90 * direction * (Time.deltaTime / rollDuration));
            yield return null;
        }

        transform.rotation = Quaternion.Euler(0, 0, Mathf.Round(transform.eulerAngles.z / 90f) * 90f);
        rb.position = transform.position;
        rb.bodyType = RigidbodyType2D.Dynamic;

        isRolling = false;
    }

    private IEnumerator GroundPound()
    {
        isGroundPounding = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0;
        yield return new WaitForSeconds(0.1f);

        rb.gravityScale = 4;
        rb.linearVelocity = new Vector2(0, -groundPoundSpeed);

        bool hasDamaged = false;
        while (!isGrounded)
        {
            if (!hasDamaged)
            {
                Collider2D hit = Physics2D.OverlapCircle(transform.position + Vector3.down * 0.5f, damageRadius, enemyLayer);
                if (hit != null && hit.GetComponent<Enemy>())
                {
                    hit.GetComponent<Enemy>().TakeDamage();
                    hasDamaged = true;
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.8f);

                    // ★ 命中敵人：噴出方塊 + 自傷
                    SpawnHealthCubes();
                    playerHealth.AttackSelfDamage(1);
                }
            }
            yield return null;
        }

        isGroundPounding = false;
    }

    /// <summary>
    /// 命中敵人時噴出血量方塊
    /// </summary>
    private void SpawnHealthCubes()
    {
        for (int i = 0; i < spawnCubeCount; i++)
        {
            // 在主角位置生成
            GameObject cube;

            if (healthCubePrefab != null)
            {
                cube = Instantiate(healthCubePrefab, transform.position, Quaternion.identity);
            }
            else
            {
                // 沒有 Prefab 就自動建立
                cube = new GameObject("HealthCube");
                cube.transform.position = transform.position;
                cube.AddComponent<HealthCube>();
            }

            // 給一個隨機方向的噴射力
            Rigidbody2D cubeRb = cube.GetComponent<Rigidbody2D>();
            if (cubeRb == null) cubeRb = cube.AddComponent<Rigidbody2D>();

            // 隨機角度往上噴（左右散開）
            float angle = Random.Range(30f, 150f); // 30~150 度之間
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            cubeRb.AddForce(dir * cubeSpawnForce, ForceMode2D.Impulse);
            cubeRb.AddTorque(Random.Range(-200f, 200f)); // 隨機旋轉
        }
    }
}