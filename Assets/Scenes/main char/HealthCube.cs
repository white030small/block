using UnityEngine;

public class HealthCube : MonoBehaviour
{
    [SerializeField] private float pickupRange = 1.5f;
    [SerializeField] private float pickupDelay = 1f;
    [SerializeField] private float lifeTime = 8f;

    private float spawnTime;
    private Transform player;
    private PlayerHealth playerHealth;
    private SpriteRenderer sr;

    private void Start()
    {
        spawnTime = Time.time;
        sr = GetComponent<SpriteRenderer>();

        // 碰撞
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();

        // 忽略主角碰撞
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerHealth = p.GetComponent<PlayerHealth>();
            foreach (Collider2D c in p.GetComponents<Collider2D>())
                Physics2D.IgnoreCollision(col, c);
        }
    }

    private void Update()
    {
        float age = Time.time - spawnTime;

        // 超時消失
        if (age > lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        // 最後 2 秒閃爍
        if (age > lifeTime - 2f && sr != null)
            sr.enabled = (Mathf.FloorToInt(Time.time * 8f) % 2 == 0);

        // 撿拾
        if (player == null) return;
        if (age < pickupDelay) return;

        if (Vector2.Distance(transform.position, player.position) < pickupRange)
        {
            if (playerHealth != null) playerHealth.CollectCube();
            Destroy(gameObject);
        }
    }
}