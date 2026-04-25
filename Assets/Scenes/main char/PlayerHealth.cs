using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 主角血量系統
/// 
/// 設定步驟：
/// 1. 掛在主角身上
/// 2. Health Slots 陣列設為 5
/// 3. 把你的 5 個 MeatHealthBar 子物件按順序拖進去
///    [0] = 第 1 格血（最先扣掉的）
///    [1] = 第 2 格血
///    [2] = 第 3 格血
///    [3] = 第 4 格血
///    [4] = 第 5 格血（最後扣掉的）
///    
///    或者反過來排也行，看你 UI 的順序
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("=== 血量設定 ===")]
    [SerializeField] private int maxHP = 5;

    [Header("=== UI 設定 ===")]
    [Tooltip("把每一格血的 GameObject 按順序拖進來")]
    [SerializeField] private GameObject[] healthSlots;

    [Header("=== 撿方塊回血 ===")]
    [SerializeField] private int cubesPerHeal = 2;

    [Header("=== 受傷無敵時間 ===")]
    [SerializeField] private float invincibleDuration = 1.5f;

    [Header("=== 事件（選用）===")]
    public UnityEvent<int, int> OnHPChanged;
    public UnityEvent OnPlayerDead;

    // 狀態
    private int currentHP;
    private int collectedCubes;
    private bool isInvincible;
    private float invincibleTimer;
    private SpriteRenderer spriteRenderer;

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public int CollectedCubes => collectedCubes;
    public bool IsInvincible => isInvincible;

    private void Awake()
    {
        currentHP = maxHP;
        collectedCubes = 0;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        UpdateUI();
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    private void Update()
    {
        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;

            if (spriteRenderer != null)
                spriteRenderer.enabled = Mathf.FloorToInt(Time.time * 10f) % 2 == 0;

            if (invincibleTimer <= 0f)
            {
                isInvincible = false;
                if (spriteRenderer != null)
                    spriteRenderer.enabled = true;
            }
        }
    }

    /// <summary>
    /// 受到傷害（被敵人攻擊時呼叫）
    /// </summary>
    public void TakeDamage(int amount = 1)
    {
        if (isInvincible || currentHP <= 0) return;

        currentHP = Mathf.Max(0, currentHP - amount);
        UpdateUI();
        OnHPChanged?.Invoke(currentHP, maxHP);

        Debug.Log($"[PlayerHealth] 受傷！HP: {currentHP}/{maxHP}");

        if (currentHP <= 0)
            Die();
        else
        {
            isInvincible = true;
            invincibleTimer = invincibleDuration;
        }
    }

    /// <summary>
    /// 攻擊時的自傷（不觸發無敵）
    /// </summary>
    public void AttackSelfDamage(int amount = 1)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
        UpdateUI();
        OnHPChanged?.Invoke(currentHP, maxHP);

        Debug.Log($"[PlayerHealth] 攻擊自傷！HP: {currentHP}/{maxHP}");

        if (currentHP <= 0)
            Die();
    }

    /// <summary>
    /// 撿到方塊時呼叫
    /// </summary>
    public void CollectCube()
    {
        collectedCubes++;
        Debug.Log($"[PlayerHealth] 撿到方塊 {collectedCubes}/{cubesPerHeal}");

        if (collectedCubes >= cubesPerHeal)
        {
            collectedCubes = 0;
            Heal(1);
        }
    }

    /// <summary>
    /// 回血
    /// </summary>
    public void Heal(int amount)
    {
        if (currentHP >= maxHP) return;

        currentHP = Mathf.Min(maxHP, currentHP + amount);
        UpdateUI();
        OnHPChanged?.Invoke(currentHP, maxHP);

        Debug.Log($"[PlayerHealth] 回血！HP: {currentHP}/{maxHP}");
    }

    /// <summary>
    /// 更新 UI：有血的格子顯示，沒血的隱藏
    /// </summary>
    private void UpdateUI()
    {
        if (healthSlots == null || healthSlots.Length == 0) return;

        for (int i = 0; i < healthSlots.Length; i++)
        {
            if (healthSlots[i] != null)
            {
                // i < currentHP 的格子顯示，其餘隱藏
                healthSlots[i].SetActive(i < currentHP);
            }
        }
    }

    private void Die()
    {
        Debug.Log("[PlayerHealth] 主角死亡！");
        UpdateUI();
        OnPlayerDead?.Invoke();
    }
}