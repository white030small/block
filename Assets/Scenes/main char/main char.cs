using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
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

    private Rigidbody2D rb;
    private bool isRolling = false;
    private bool isGroundPounding = false;
    private bool isDashing = false;

    private float dashCooldownTimer = 0f;
    private float lastMoveDirection = 1f; // 記憶最後按下的方向

    private int contactCount = 0;
    private bool isGrounded => contactCount > 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
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
        // 假設你已經有一個記錄最後方向的變數，例如 lastMoveDirection (1 或 -1)
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isRolling && !isDashing)
        {
            // 1. 重置垂直速度，確保起跳高度一致
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);

            // 2. 定義斜跳向量：(水平力 * 方向, 垂直跳躍力
            Vector2 jumpVector = new Vector2(lastMoveDirection * jumpHorizontalForce, jumpForce);

            // 3. 施加力
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

        // 1. 關閉物理模擬
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;

        // 2. 執行旋轉
        float elapsed = 0f;
        float half = cubeSize / 2f;
        Vector3 pivot = transform.position + new Vector3(direction * half, -half, 0f);

        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;
            transform.RotateAround(pivot, Vector3.forward, -90 * direction * (Time.deltaTime / rollDuration));
            yield return null;
        }

        // 3. 修正：旋轉完後，將座標對齊到完美格子
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Round(transform.eulerAngles.z / 90f) * 90f);

        // --- 核心修正：將 Transform 的位置告知 Rigidbody ---
        // 這樣物理引擎就不會以為物體在「偷跑」
        rb.position = transform.position;

        // 4. 開啟物理模擬
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

        yield return new WaitUntil(() => isGrounded);
        isGroundPounding = false;
    }
}