using UnityEngine;

/// <summary>
/// 噴出的血量方塊（可撿拾）
/// 主角下壓命中敵人時會噴出，撿 2 個回 1 格血
/// 
/// 此腳本會自動建立 Sprite 和 Collider，不需要額外設定
/// </summary>
public class HealthCube : MonoBehaviour
{
    [Header("=== 方塊設定 ===")]
    [SerializeField] private float lifeTime = 8f;
    [SerializeField] private float collectRadius = 0.5f;
    [SerializeField] private Color cubeColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Header("=== 噴射設定 ===")]
    [SerializeField] private float bounceHeight = 0.3f;
    [SerializeField] private float bounceSpeed = 2f;

    private float spawnTime;
    private Vector3 basePosition;
    private bool isSettled; // 落地後開始彈跳動畫
    private Rigidbody2D rb;

    private void Start()
    {
        spawnTime = Time.time;

        // 自動建立外觀
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        sr.sprite = CreateSquareSprite();
        sr.color = cubeColor;
        sr.sortingOrder = 5;
        transform.localScale = Vector3.one * 0.3f;

        // 加碰撞（Trigger 用來偵測撿拾）
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = Vector2.one * (collectRadius / 0.3f);

        // 加 Rigidbody 讓它有物理彈跳
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = false;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // 加一個實體碰撞的 Collider（跟地面互動）
        CircleCollider2D physicsCol = gameObject.AddComponent<CircleCollider2D>();
        physicsCol.isTrigger = false;
        physicsCol.radius = 0.4f;
    }

    private void Update()
    {
        // 超時消失（接近消失前閃爍）
        float elapsed = Time.time - spawnTime;

        if (elapsed > lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        // 最後 2 秒閃爍提示
        if (elapsed > lifeTime - 2f)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.enabled = Mathf.FloorToInt(Time.time * 8f) % 2 == 0;
        }

        // 落地後微微上下彈跳
        if (isSettled)
        {
            float bounce = Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
            transform.position = basePosition + Vector3.up * bounce;
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        // 落地後停住，開始浮動動畫
        if (!isSettled && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            basePosition = transform.position;
            isSettled = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 主角碰到就撿起來
        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.CollectCube();
            Destroy(gameObject);
        }
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(4, 4);
        Color[] colors = new Color[16];
        for (int i = 0; i < 16; i++) colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }
}