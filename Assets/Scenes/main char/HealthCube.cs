using UnityEngine;

/// <summary>
/// 可撿拾的肉塊
/// 落地後浮動，主角走近自動撿起
/// 撿 2 個回 1 格血
/// </summary>
public class HealthCube : MonoBehaviour
{
    [Header("=== 設定 ===")]
    [SerializeField] private float lifeTime = 8f;
    [SerializeField] private float pickupRange = 1.2f;
    [SerializeField] private float cubeScale = 0.4f;
    [SerializeField] private Color cubeColor = new Color(0.9f, 0.15f, 0.15f, 1f);
    [SerializeField] private float bounceHeight = 0.15f;
    [SerializeField] private float bounceSpeed = 3f;

    [SerializeField] private float pickupDelay = 0.8f;

    private float spawnTime;
    private bool isSettled;
    private Vector3 settledPos;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Transform playerTransform;
    private PlayerHealth playerHealth;

    private void Start()
    {
        spawnTime = Time.time;

        // --- 外觀 ---
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        if (sr.sprite == null) sr.sprite = MakeSprite();
        sr.color = cubeColor;
        sr.sortingOrder = 5;
        transform.localScale = Vector3.one * cubeScale;

        // --- 物理 ---
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
        }
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = false;

        // --- 碰撞（非 Trigger，跟地面互動）---
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = false;
        col.size = Vector2.one;

        // --- 找主角 ---
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }

        if (playerTransform == null)
        {
            // 備用：找 PlayerHealth 元件
            PlayerHealth ph = FindObjectOfType<PlayerHealth>();
            if (ph != null)
            {
                playerTransform = ph.transform;
                playerHealth = ph;
            }
        }
    }

    private void Update()
    {
        float age = Time.time - spawnTime;

        // --- 超時消失 ---
        if (age > lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        // --- 最後 2 秒閃爍 ---
        if (age > lifeTime - 2f && sr != null)
        {
            sr.enabled = Mathf.FloorToInt(Time.time * 8f) % 2 == 0;
        }

        // --- 落地後浮動 ---
        if (isSettled)
        {
            float bounce = Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
            transform.position = settledPos + Vector3.up * bounce;
        }

        // --- 撿拾偵測（噴出後等一下才能撿）---
        if (playerTransform != null && (Time.time - spawnTime) > pickupDelay)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist < pickupRange)
            {
                if (playerHealth != null)
                {
                    playerHealth.CollectCube();
                    Debug.Log("[HealthCube] 被撿起來了！");
                }
                Destroy(gameObject);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        // 碰到任何東西（地面）就停下來
        if (!isSettled && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            settledPos = transform.position;
            isSettled = true;
        }
    }

    private Sprite MakeSprite()
    {
        Texture2D tex = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }
}