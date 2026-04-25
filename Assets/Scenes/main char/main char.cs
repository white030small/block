using UnityEngine;
using System.Collections;

/// <summary>
/// 方塊角色控制器 - mainchar
/// 移動：以四個角為支點翻滾（← → 或 A/D）
/// 跳躍：空白鍵（按一次跳一次）
/// 下壓：空中滑鼠左鍵
/// 
/// 設定步驟：
/// 1. 建立一個正方形 Sprite，掛上此腳本
/// 2. 地面物件的 Layer 設為 "Ground"（或你自訂的名稱）
/// 3. Inspector 中 Ground Layer 選擇對應的 Layer
/// 4. 地面和方塊都要有 Collider2D
/// 5. （建議）地面加上 Physics Material 2D，Friction=0.4, Bounciness=0
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class mainchar : MonoBehaviour
{
    [Header("=== 方塊設定 ===")]
    [Tooltip("方塊邊長（需和 Sprite 或 Scale 一致）")]
    [SerializeField] private float cubeSize = 1f;

    [Header("=== 翻滾設定 ===")]
    [Tooltip("翻滾一次（90度）的時間")]
    [SerializeField] private float rollDuration = 0.2f;

    [Header("=== 跳躍設定 ===")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float maxFallSpeed = 20f;
    [SerializeField] private float fallGravityScale = 5f;
    [SerializeField] private float normalGravityScale = 3f;

    [Header("=== 下壓設定 ===")]
    [SerializeField] private float groundPoundSpeed = 25f;
    [SerializeField] private float groundPoundHangTime = 0.1f;

    [Header("=== 地面偵測 ===")]
    [SerializeField] private LayerMask groundLayer;

    // 狀態
    private Rigidbody2D rb;
    private bool isRolling;
    private bool isGrounded;
    private bool isGroundPounding;
    private bool isHanging;
    private bool hasJumped; // 防止連跳：跳過一次後必須落地才能再跳

    // 碰撞偵測用
    private int groundContactCount;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = normalGravityScale;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; // 讓移動更滑順
    }

    // ============================================================
    //  地面偵測（用碰撞事件，比 Raycast 更可靠）
    // ============================================================
    private void OnCollisionEnter2D(Collision2D col)
    {
        // ★ DEBUG：看碰撞到底有沒有發生，以及對方的 Layer 是什麼
        Debug.Log($"[碰撞] 碰到: {col.gameObject.name}, Layer: {col.gameObject.layer} ({LayerMask.LayerToName(col.gameObject.layer)}), groundLayer.value: {groundLayer.value}, isTrigger: {col.collider.isTrigger}");

        if (IsGroundLayer(col.gameObject.layer))
        {
            foreach (ContactPoint2D contact in col.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    groundContactCount++;
                    Debug.Log($"[Ground] Enter! contacts={groundContactCount} obj={col.gameObject.name}");
                    break;
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D col)
    {
        if (IsGroundLayer(col.gameObject.layer))
        {
            groundContactCount = Mathf.Max(0, groundContactCount - 1);
            Debug.Log($"[Ground] Exit! contacts={groundContactCount} obj={col.gameObject.name}");
        }
    }

    private bool IsGroundLayer(int layer)
    {
        return (groundLayer.value & (1 << layer)) != 0;
    }

    private void Update()
    {
        isGrounded = groundContactCount > 0;

        // ★ DEBUG：在 Console 觀察狀態，找到問題後刪掉這行
        Debug.Log($"[mainchar] grounded={isGrounded} rolling={isRolling} contacts={groundContactCount} bodyType={rb.bodyType} vel={rb.linearVelocity} pos={transform.position}");

        // 落地時重置狀態
        if (isGrounded)
        {
            hasJumped = false;

            if (isGroundPounding)
            {
                isGroundPounding = false;
                rb.gravityScale = normalGravityScale;
                OnGroundPoundLand();
            }
        }

        // ---------- 翻滾移動 ----------
        if (isGrounded && !isRolling && !isGroundPounding && !hasJumped)
        {
            float input = Input.GetAxisRaw("Horizontal");
            if (input > 0.1f)
                StartCoroutine(Roll(1));
            else if (input < -0.1f)
                StartCoroutine(Roll(-1));
        }

        // ---------- 跳躍 ----------
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isRolling && !hasJumped && !isGroundPounding)
        {
            Jump();
        }

        // ---------- 下壓（空中 + 滑鼠左鍵）----------
        if (Input.GetMouseButtonDown(0) && !isGrounded && !isGroundPounding && !isHanging)
        {
            StartCoroutine(GroundPound());
        }

        // ---------- 下墜加速 + 限速 ----------
        if (!isGrounded && !isRolling && !isHanging && !isGroundPounding)
        {
            // 下墜時加重重力，讓手感更好
            if (rb.linearVelocity.y < 0f)
                rb.gravityScale = fallGravityScale;
            else
                rb.gravityScale = normalGravityScale;

            // 限制最大下墜速度
            if (rb.linearVelocity.y < -maxFallSpeed)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
        }
    }

    // ============================================================
    //  翻滾（以底部角為支點旋轉 90 度）
    // ============================================================
    private IEnumerator Roll(int direction)
    {
        isRolling = true;
        Debug.Log($"[Roll] 開始翻滾 direction={direction}");

        // 計算支點：向右用右下角，向左用左下角
        float half = cubeSize / 2f;
        Vector3 pivot = transform.position + new Vector3(direction * half, -half, 0f);

        // 切換為 Kinematic，避免物理引擎干擾旋轉動畫
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        float totalAngle = -90f * direction;
        float rotated = 0f;
        float speed = totalAngle / rollDuration;

        while (Mathf.Abs(rotated) < 90f)
        {
            float step = speed * Time.deltaTime;

            if (Mathf.Abs(rotated + step) > 90f)
                step = totalAngle - rotated;

            transform.RotateAround(pivot, Vector3.forward, step);
            rotated += step;

            yield return null;
        }

        // 校正角度
        SnapRotation();

        // 恢復為 Dynamic
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true; // bodyType 切換後要重新設定
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = normalGravityScale;

        isRolling = false;
        Debug.Log($"[Roll] 翻滾結束 grounded={isGrounded} contacts={groundContactCount}");
    }

    private void SnapRotation()
    {
        float z = transform.eulerAngles.z;
        z = Mathf.Round(z / 90f) * 90f;
        transform.eulerAngles = new Vector3(0f, 0f, z);

        // 位置微調，避免浮點漂移
        Vector3 pos = transform.position;
        float snapUnit = cubeSize / 2f;
        pos.x = Mathf.Round(pos.x / snapUnit) * snapUnit;
        pos.y = Mathf.Round(pos.y * 200f) / 200f;
        transform.position = pos;
    }

    // ============================================================
    //  跳躍
    // ============================================================
    private void Jump()
    {
        hasJumped = true;
        rb.linearVelocity = new Vector2(0f, jumpForce);
    }

    // ============================================================
    //  下壓
    // ============================================================
    private IEnumerator GroundPound()
    {
        isHanging = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        yield return new WaitForSeconds(groundPoundHangTime);

        isHanging = false;
        isGroundPounding = true;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.down * groundPoundSpeed;
    }

    private void OnGroundPoundLand()
    {
        rb.gravityScale = normalGravityScale;
        Debug.Log("下壓落地！");
    }

    // ============================================================
    //  Gizmos
    // ============================================================
    private void OnDrawGizmosSelected()
    {
        float half = cubeSize / 2f;

        // 支點位置
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + new Vector3(half, -half, 0f), 0.06f);
        Gizmos.DrawWireSphere(transform.position + new Vector3(-half, -half, 0f), 0.06f);

        // 地面狀態
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position + Vector3.down * half, new Vector3(cubeSize, 0.05f, 0f));
    }
}