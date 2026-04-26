using UnityEngine;
using System.Collections;

/// <summary>
/// 主角血量系統
/// 
/// 設定步驟：
/// 1. 掛在主角身上（mainchar 會自動要求）
/// 2. Inspector → Health Slots → Size 設 5
/// 3. 把 MeatHealthBar_0 ~ MeatHealthBar_0(4) 按順序拖進去
///    [0] = 第一格（最後才消失）
///    [4] = 第五格（最先消失）
///    ※如果扣血順序反了，就反過來拖
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("=== 血量 ===")]
    [SerializeField] private int maxHP = 5;

    [Header("=== 血量 UI ===")]
    [Tooltip("5 個 MeatHealthBar 物件，按順序拖入")]
    [SerializeField] private GameObject[] healthSlots;

    [Header("=== 回血 ===")]
    [Tooltip("撿幾個肉塊回 1 格血")]
    [SerializeField] private int cubesPerHeal = 2;

    [Header("=== 無敵 ===")]
    [SerializeField] private float invincibleDuration = 1.5f;

    [Header("=== 縮小 ===")]
    [Tooltip("1 格血時縮到原始大小的幾倍（0.4 = 縮到 40%）")]
    [SerializeField] private float minScaleRatio = 0.4f;
    [SerializeField] private float shrinkSpeed = 5f;

    [Header("=== 被打掉肉塊 ===")]
    [SerializeField] private float meatLaunchForce = 6f;

    // --- 內部狀態 ---
    private int currentHP;
    private int collectedCubes;
    private bool isInvincible;
    private float invincibleTimer;
    private float initialScale;   // 開局時自動記錄角色大小
    private float targetScale;
    private SpriteRenderer spriteRenderer;

    // 警告文字
    private GameObject warningObj;
    private bool showingWarning;

    // --- 公開屬性 ---
    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public int CollectedCubes => collectedCubes;
    public bool IsInvincible => isInvincible;

    // ============================================================
    //  初始化
    // ============================================================
    private void Awake()
    {
        currentHP = maxHP;
        collectedCubes = 0;
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 自動記錄角色原始大小
        initialScale = Mathf.Abs(transform.localScale.y);
        targetScale = initialScale;
        Debug.Log($"[PlayerHealth] 記錄原始大小: {initialScale}");
    }

    private void Start()
    {
        // 確保開局 UI 正確
        RefreshUI();
        Debug.Log($"[PlayerHealth] 初始化完成 HP={currentHP}/{maxHP} slots={healthSlots?.Length}");
    }

    // ============================================================
    //  Update
    // ============================================================
    private void Update()
    {
        // 無敵閃爍
        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;

            if (spriteRenderer != null)
                spriteRenderer.enabled = (Mathf.FloorToInt(Time.time * 10f) % 2 == 0);

            if (invincibleTimer <= 0f)
            {
                isInvincible = false;
                if (spriteRenderer != null)
                    spriteRenderer.enabled = true;
            }
        }

        // 平滑縮放
        float curScale = Mathf.Abs(transform.localScale.y);
        if (Mathf.Abs(curScale - targetScale) > 0.01f)
        {
            float next = Mathf.Lerp(curScale, targetScale, shrinkSpeed * Time.deltaTime);
            float signX = Mathf.Sign(transform.localScale.x);
            transform.localScale = new Vector3(signX * next, next, 1f);
        }
    }

    // ============================================================
    //  被攻擊（扣 1 格血 + 掉肉塊）
    // ============================================================
    public void TakeDamage(int amount = 1)
    {
        if (isInvincible || currentHP <= 0) return;

        // 掉肉塊
        LaunchMeatCube();

        // 扣血
        currentHP = Mathf.Max(0, currentHP - amount);
        Debug.Log($"[PlayerHealth] 受傷！HP: {currentHP}/{maxHP}");

        // 更新顯示
        RefreshUI();

        if (currentHP <= 0)
        {
            Debug.Log("[PlayerHealth] 死亡！");
        }
        else
        {
            isInvincible = true;
            invincibleTimer = invincibleDuration;
        }
    }

    /// <summary>
    /// 下壓自傷（不觸發無敵、不掉肉塊，因為下壓已經自己噴了）
    /// </summary>
    public void SelfDamage(int amount = 1)
    {
        if (currentHP <= 0) return;

        currentHP = Mathf.Max(0, currentHP - amount);
        Debug.Log($"[PlayerHealth] 自傷！HP: {currentHP}/{maxHP}");

        RefreshUI();

        if (currentHP <= 0)
        {
            Debug.Log("[PlayerHealth] 死亡！");
        }
    }

    // ============================================================
    //  撿肉塊
    // ============================================================
    public void CollectCube()
    {
        collectedCubes++;
        Debug.Log($"[PlayerHealth] 肉塊 {collectedCubes}/{cubesPerHeal}");

        if (collectedCubes >= cubesPerHeal)
        {
            collectedCubes = 0;
            Heal(1);
        }
    }

    // ============================================================
    //  回血
    // ============================================================
    public void Heal(int amount)
    {
        if (currentHP >= maxHP) return;

        currentHP = Mathf.Min(maxHP, currentHP + amount);
        Debug.Log($"[PlayerHealth] 回血！HP: {currentHP}/{maxHP}");

        RefreshUI();
    }

    // ============================================================
    //  下壓檢查（1 格血禁止）
    // ============================================================
    public bool TryGroundPound()
    {
        if (currentHP > 1)
            return true;

        // 1 格血 → 顯示警告，不下壓
        if (!showingWarning)
            StartCoroutine(ShowWarningText("感覺會死掉"));

        return false;
    }

    // ============================================================
    //  內部：更新 UI + 縮放
    // ============================================================
    private void RefreshUI()
    {
        // 更新血量格子
        if (healthSlots != null)
        {
            for (int i = 0; i < healthSlots.Length; i++)
            {
                if (healthSlots[i] != null)
                {
                    bool show = (i < currentHP);
                    healthSlots[i].SetActive(show);
                }
            }
            Debug.Log($"[PlayerHealth] UI 更新：顯示 {currentHP} 格");
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] healthSlots 是空的！請在 Inspector 拖入 MeatHealthBar");
        }

        // 更新縮放目標
        float minScale = initialScale * minScaleRatio;
        if (currentHP <= 1)
            targetScale = minScale;
        else
        {
            float ratio = (float)(currentHP - 1) / (float)(maxHP - 1);
            targetScale = Mathf.Lerp(minScale, initialScale, ratio);
        }
        Debug.Log($"[PlayerHealth] 縮放目標: {targetScale} (原始: {initialScale})");
    }

    // ============================================================
    //  內部：掉肉塊
    // ============================================================
    private void LaunchMeatCube()
    {
        GameObject meat = new GameObject("DroppedMeat");
        meat.transform.position = transform.position + Vector3.up * 0.5f;

        Rigidbody2D meatRb = meat.AddComponent<Rigidbody2D>();
        meatRb.gravityScale = 3f;
        meatRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        meat.AddComponent<HealthCube>();

        float angle = Random.Range(50f, 130f);
        Vector2 dir = new Vector2(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            Mathf.Sin(angle * Mathf.Deg2Rad)
        );
        meatRb.AddForce(dir * meatLaunchForce, ForceMode2D.Impulse);
        meatRb.AddTorque(Random.Range(-100f, 100f));
    }

    // ============================================================
    //  內部：警告文字
    // ============================================================
    private IEnumerator ShowWarningText(string msg)
    {
        showingWarning = true;

        // 建立文字物件
        if (warningObj == null)
        {
            warningObj = new GameObject("Warning");
            TextMesh tm = warningObj.AddComponent<TextMesh>();
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.fontSize = 36;
            tm.characterSize = 0.15f;
            tm.color = Color.red;
            warningObj.GetComponent<MeshRenderer>().sortingOrder = 100;
        }

        TextMesh text = warningObj.GetComponent<TextMesh>();
        text.text = msg;
        text.color = new Color(1f, 0f, 0f, 1f);
        warningObj.transform.position = transform.position + Vector3.up * 1.5f;
        warningObj.SetActive(true);

        // 往上飄 + 淡出
        Vector3 start = warningObj.transform.position;
        float t = 0f;
        float dur = 1.5f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float progress = t / dur;
            warningObj.transform.position = start + Vector3.up * progress;

            Color c = text.color;
            c.a = 1f - progress;
            text.color = c;
            yield return null;
        }

        warningObj.SetActive(false);
        text.color = new Color(1f, 0f, 0f, 1f);
        showingWarning = false;
    }
}