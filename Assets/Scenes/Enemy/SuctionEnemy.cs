using UnityEngine;
using System.Collections;

/// <summary>
/// 吸引型敵人（含動畫整合）
/// 
/// 動畫狀態：
///   Walk    - 巡邏走動（預設狀態）
///   Attack  - 吸引主角中
///   Eat     - 吸到主角，吃掉扣血
///   Idle    - 停下來待機
///
/// Animator 設定：
///   Parameters（需要修改的）：
///     - move           (Bool)    → 控制走路動畫
///     - attack         (Bool)    → ★改成 Bool！吸引時持續播放
///     - attack_fiish   (Trigger) → 觸發吃掉動畫（4秒沒掙脫）
///
///   Transitions（箭頭連接）：
///     Idle → Walk        : move = true
///     Walk → Idle        : move = false
///     Any State → Attack : attack = true
///     Attack → Eat       : attack_fiish trigger
///     Attack → Walk      : attack = false（掙脫成功）
///     Eat → Idle         : Has Exit Time ✓（動畫播完自動切）
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
public class SuctionEnemy : MonoBehaviour
{
    [Header("=== 巡邏設定 ===")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float minWalkTime = 1f;
    [SerializeField] private float maxWalkTime = 3f;
    [SerializeField] private float minIdleTime = 0.5f;
    [SerializeField] private float maxIdleTime = 2f;

    [Header("=== 吸引攻擊設定 ===")]
    [SerializeField] private float detectRange = 6f;
    [SerializeField] private float suctionForce = 12f;
    [Tooltip("主角必須在幾秒內掙脫，否則被吃掉")]
    [SerializeField] private float suctionDuration = 4f;
    [SerializeField] private int damage = 1;

    [Header("=== 掙脫設定 ===")]
    [SerializeField] private int mashCountToEscape = 10;
    [SerializeField] private float mashDecayRate = 3f;
    [SerializeField] private float suctionCooldown = 3f;
    [SerializeField] private float escapeKnockback = 10f;

    [Header("=== 吃掉動畫設定 ===")]
    [Tooltip("吃掉動畫播放時間（結束後吐出主角）")]
    [SerializeField] private float eatDuration = 1f;
    [Tooltip("掙脫時主角消失的時間")]
    [SerializeField] private float escapeHideDuration = 0.3f;

    [Header("=== 牆壁/懸崖偵測 ===")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private float cliffCheckDepth = 1.5f;

    [Header("=== 參照 ===")]
    [SerializeField] private Transform player;
    [SerializeField] private MashProgressBar mashProgressBar;

    // 狀態機
    private enum State { Patrol, Idle, Suction, Eating, Cooldown }
    private State currentState = State.Idle;

    // 內部變數
    private float stateTimer;
    private int patrolDirection = 1;
    private float cooldownTimer;
    private float currentMashCount;
    private float suctionTimer;
    private Rigidbody2D playerRb;
    private SpriteRenderer playerRenderer;
    private Collider2D playerCollider;

    // 動畫（對應你的 Animator Parameters）
    private Animator animator;
    private static readonly int AnimMove = Animator.StringToHash("move");
    private static readonly int AnimAttack = Animator.StringToHash("attack");
    private static readonly int AnimAttackFinish = Animator.StringToHash("attack_fiish");

    private void Awake()
    {
        animator = GetComponent<Animator>();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogError("[SuctionEnemy] 找不到主角！請拖入 Player 欄位或加 'Player' Tag。");
        }

        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody2D>();
            playerRenderer = player.GetComponent<SpriteRenderer>();
            playerCollider = player.GetComponent<Collider2D>();
        }

        EnterIdle();
    }

    private void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Idle:
                UpdateIdle(dist);
                break;
            case State.Patrol:
                UpdatePatrol(dist);
                break;
            case State.Suction:
                UpdateSuction(dist);
                break;
            case State.Eating:
                // 等動畫播完，由 Coroutine 控制
                break;
            case State.Cooldown:
                UpdateCooldown();
                break;
        }
    }

    // ============================================================
    //  待機
    // ============================================================
    private void EnterIdle()
    {
        currentState = State.Idle;
        stateTimer = Random.Range(minIdleTime, maxIdleTime);
        animator.SetBool(AnimMove, false);
        animator.SetBool(AnimAttack, false);
        animator.ResetTrigger(AnimAttackFinish);
        if (mashProgressBar != null) mashProgressBar.Hide();
    }

    private void UpdateIdle(float dist)
    {
        if (dist <= detectRange)
        {
            EnterSuction();
            return;
        }

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
            EnterPatrol();
    }

    // ============================================================
    //  巡邏
    // ============================================================
    private void EnterPatrol()
    {
        currentState = State.Patrol;
        stateTimer = Random.Range(minWalkTime, maxWalkTime);
        patrolDirection = Random.value > 0.5f ? 1 : -1;
        animator.SetBool(AnimMove, true);
    }

    private void UpdatePatrol(float dist)
    {
        if (dist <= detectRange)
        {
            EnterSuction();
            return;
        }

        if (CheckWall() || CheckCliff())
            patrolDirection *= -1;

        // 移動
        transform.position += Vector3.right * patrolDirection * patrolSpeed * Time.deltaTime;

        // 面朝方向
        FaceDirection(patrolDirection);

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
            EnterIdle();
    }

    // ============================================================
    //  吸引攻擊
    // ============================================================
    private void EnterSuction()
    {
        currentState = State.Suction;
        currentMashCount = 0f;
        suctionTimer = suctionDuration;
        
        // 重置殘留的 trigger，避免下次攻擊直接跳到吃掉動畫
        animator.ResetTrigger(AnimAttackFinish);
        animator.SetBool(AnimMove, false);
        animator.SetBool(AnimAttack, true);

        // 面朝主角
        float dir = player.position.x - transform.position.x;
        FaceDirection(dir >= 0 ? 1 : -1);
    }

    private void UpdateSuction(float dist)
    {
        // 主角跑出範圍 → 掙脫成功
        if (dist > detectRange * 1.2f)
        {
            OnPlayerEscape();
            return;
        }

        // 持續面朝主角
        float dir = player.position.x - transform.position.x;
        FaceDirection(dir >= 0 ? 1 : -1);

        // 施加吸引力
        if (playerRb != null)
        {
            Vector2 pullDir = ((Vector2)transform.position - (Vector2)player.position).normalized;
            playerRb.AddForce(pullDir * suctionForce);
        }

        // ★ 先處理掙脫（玩家操作優先）
        currentMashCount -= mashDecayRate * Time.deltaTime;
        currentMashCount = Mathf.Max(0f, currentMashCount);

        if (Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1))
            currentMashCount += 1f;

        // 更新進度條
        if (mashProgressBar != null)
            mashProgressBar.SetProgress(currentMashCount / mashCountToEscape);

        // 掙脫成功 → 優先判定，不會被吃
        if (currentMashCount >= mashCountToEscape)
        {
            OnPlayerEscape();
            return;
        }

        // ★ 最後才檢查倒數計時
        suctionTimer -= Time.deltaTime;
        if (suctionTimer <= 0f)
        {
            EnterEating();
            return;
        }
    }

    // ============================================================
    //  吃掉（攻擊命中）
    // ============================================================
    private void EnterEating()
    {
        currentState = State.Eating;

        if (mashProgressBar != null) mashProgressBar.Hide();

        // 主角消失（被吃進去）
        SetPlayerVisible(false);

        // 把主角固定在敵人身上
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.bodyType = RigidbodyType2D.Kinematic;
            player.position = transform.position;
        }

        Debug.Log($"[SuctionEnemy] 吃掉主角！造成 {damage} 點傷害");
        player.GetComponent<PlayerHealth>()?.TakeDamage(damage);

        StartCoroutine(EatingSequence());
    }

    private IEnumerator EatingSequence()
    {
        // 先關掉 attack，等一幀讓 Animator 離開 Attack 狀態
        animator.SetBool(AnimAttack, false);
        yield return null;

        // 現在才觸發吃掉動畫
        animator.SetTrigger(AnimAttackFinish);

        // 等吃掉動畫播完
        yield return new WaitForSeconds(eatDuration);

        // 計算吐出位置：敵人面朝的反方向，水平彈出
        float spitDir = transform.localScale.x >= 0 ? -1f : 1f;
        Vector2 spitPos = (Vector2)transform.position + Vector2.right * spitDir * 1.5f;

        // 動畫結束 → 吐出主角
        if (playerRb != null)
        {
            player.position = spitPos;
            playerRb.bodyType = RigidbodyType2D.Dynamic;
            playerRb.linearVelocity = new Vector2(spitDir * escapeKnockback, 2f); // 稍微往上拋，不會掉進地板
        }

        // 主角重新出現
        SetPlayerVisible(true);

        EnterCooldown();
    }

    // ============================================================
    //  掙脫成功
    // ============================================================
    private void OnPlayerEscape()
    {
        Debug.Log("[SuctionEnemy] 主角掙脫了！");
        animator.SetBool(AnimAttack, false); // 停止吸引動畫
        animator.SetBool(AnimMove, true);

        currentMashCount = 0f;
        if (mashProgressBar != null) mashProgressBar.Hide();

        StartCoroutine(EscapeSequence());
    }

    private IEnumerator EscapeSequence()
    {
        // 主角消失一瞬間（掙脫脫出的瞬間）
        SetPlayerVisible(false);

        yield return new WaitForSeconds(escapeHideDuration);

        // 主角重新出現並被彈開（水平方向 + 微微向上）
        SetPlayerVisible(true);

        if (playerRb != null)
        {
            float escapeDir = player.position.x >= transform.position.x ? 1f : -1f;
            playerRb.linearVelocity = new Vector2(escapeDir * escapeKnockback, 2f);
        }

        EnterCooldown();
    }

    // ============================================================
    //  冷卻
    // ============================================================
    private void EnterCooldown()
    {
        currentState = State.Cooldown;
        cooldownTimer = suctionCooldown;
        animator.SetBool(AnimAttack, false);
        animator.SetBool(AnimMove, false);
    }

    private void UpdateCooldown()
    {
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
            EnterIdle();
    }

    // ============================================================
    //  工具
    // ============================================================

    /// <summary>
    /// 控制主角的顯示/隱藏（被吃掉或掙脫時使用）
    /// </summary>
    private void SetPlayerVisible(bool visible)
    {
        if (playerRenderer != null)
            playerRenderer.enabled = visible;

        // 隱藏時也關閉碰撞，避免吃進去後還被其他東西打到
        if (playerCollider != null)
            playerCollider.enabled = visible;
    }

    private void FaceDirection(int dir)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (dir >= 0 ? 1 : -1);
        transform.localScale = scale;
    }

    private bool CheckWall()
    {
        Vector2 origin = (Vector2)transform.position;
        return Physics2D.Raycast(origin, Vector2.right * patrolDirection, wallCheckDistance, groundLayer).collider != null;
    }

    private bool CheckCliff()
    {
        float half = 0.5f;
        Vector2 origin = (Vector2)transform.position + Vector2.right * patrolDirection * half + Vector2.down * 0.1f;
        return Physics2D.Raycast(origin, Vector2.down, cliffCheckDepth, groundLayer).collider == null;
    }

    // ============================================================
    //  Gizmos
    // ============================================================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.8f); // 敵人中心

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * patrolDirection * wallCheckDistance);

        Gizmos.color = Color.magenta;
        float half = 0.5f;
        Vector3 cliffOrigin = transform.position + Vector3.right * patrolDirection * half + Vector3.down * 0.1f;
        Gizmos.DrawLine(cliffOrigin, cliffOrigin + Vector3.down * cliffCheckDepth);
    }
}