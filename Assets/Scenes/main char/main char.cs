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

    [Header("=== 下壓噴肉塊 ===")]
    [SerializeField] private int spawnCubeCount = 2;
    [Tooltip("肉塊噴射力道")]
    [SerializeField] private float cubeSpawnForce = 10f;

    // 元件
    private bool isDead = false; // 死亡總開關
    private Rigidbody2D rb;
    private PlayerHealth playerHealth;

    // 狀態
    private bool isRolling = false;
    public bool isGroundPounding = false;
    private bool isDashing = false;
    private float dashCooldownTimer = 0f;
    private float lastMoveDirection = 1f;

    // 地面偵測
    private int contactCount = 0;
    private bool isGrounded => contactCount > 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerHealth = GetComponent<PlayerHealth>();

        // 訂閱事件
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath += HandleDeath;
            Debug.Log("[mainchar] 已成功訂閱死亡事件");
        }
    }
    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("[mainchar] 執行死亡鎖定...");

        // 1. 徹底停止所有行為
        StopAllCoroutines();
        // ★ 關鍵順序：先關閉物理速度，再設為 Static
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
        rb.bodyType = RigidbodyType2D.Static;

        // 2. 讓主角變成「幽靈」，不會再觸發任何碰撞 (防止回血)
        GetComponent<Collider2D>().enabled = false;
        if (playerHealth != null) playerHealth.enabled = false;

        // 3. 切換攝影機
        CameraFollow2D cam = Camera.main.GetComponent<CameraFollow2D>();
        GameObject boss = GameObject.FindGameObjectWithTag("Boss"); // 記得將 Boss 的 Tag 設為 Boss

        if (cam != null && boss != null)
        {
            // 將 100f 改為你需要的大小 (例如 15f 或 20f)，數值越大畫面越遠
            cam.SwitchTarget(boss.transform, 100f);
        }

        // 4. 最後才關閉腳本
        this.enabled = false;
    }
    private void OnDestroy()
    {
        // 養成好習慣：取消訂閱
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath -= HandleDeath;
        }
    }
    

    private void OnCollisionEnter2D(Collision2D collision) { contactCount++; }
    private void OnCollisionExit2D(Collision2D collision) { contactCount--; }

    private void Update()
    {
        if (isDead) return;

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

        // 記住原始重力
        float originalGravity = rb.gravityScale;

        // ★ 下壓瞬間：噴肉塊 + 扣 1 格血
        SpawnMeatCubes();
        if (playerHealth != null)
        {
            playerHealth.SelfDamage(1);
            Debug.Log("[mainchar] 下壓自傷 1 格！");
        }

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

        // ★ 落地後還原重力
        rb.gravityScale = originalGravity;
        isGroundPounding = false;
    }

    // ============================================================
    //  噴肉塊
    // ============================================================
    private void SpawnMeatCubes()
    {
        Debug.Log($"[mainchar] 噴出 {spawnCubeCount} 個肉塊！");

        for (int i = 0; i < spawnCubeCount; i++)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.3f;

            GameObject cube = new GameObject($"MeatCube_{i}");
            cube.transform.position = spawnPos;
            cube.transform.localScale = Vector3.one * 0.5f;

            // 外觀：紅色方塊
            SpriteRenderer sr = cube.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
            sr.color = Color.red;
            sr.sortingOrder = 5;

            // 物理
            Rigidbody2D cubeRb = cube.AddComponent<Rigidbody2D>();
            cubeRb.gravityScale = 3f;
            cubeRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // 撿拾邏輯
            cube.AddComponent<HealthCube>();

            // 左右噴射（第一個往左，第二個往右）
            float angle = (i == 0) ? Random.Range(110f, 150f) : Random.Range(30f, 70f);
            Vector2 dir = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );
            cubeRb.AddForce(dir * cubeSpawnForce, ForceMode2D.Impulse);
            cubeRb.AddTorque(Random.Range(-150f, 150f));
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