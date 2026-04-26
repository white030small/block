using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("=== 生成設定 ===")]
    public GameObject enemyPrefab;
    public Transform player;
    public float spawnInterval = 3f;
    public float minSpawnDistance = 5f;
    public Vector2 spawnArea = new Vector2(20f, 10f);

    [Header("=== 數量限制 ===")]
    public int maxEnemyCount = 5;

    private float timer;

    private void Start()
    {
        timer = spawnInterval;
    }

    private void Update()
    {
        GameObject[] currentEnemies = GameObject.FindGameObjectsWithTag("Enemy");

        // 將這行解除註解，看看 Console 寫了什麼
        Debug.Log($"[生成器] 目前場上敵人數量: {currentEnemies.Length}");

        if (currentEnemies.Length < maxEnemyCount)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                SpawnEnemy();
                timer = spawnInterval;
            }
        }
    }

    private void SpawnEnemy()
    {
        Vector2 spawnPos = Vector2.zero;
        bool validPos = false;
        int attempts = 0;

        while (!validPos && attempts < 10)
        {
            // X 座標維持隨機，Y 座標強制固定為 1.2f
            float randomX = UnityEngine.Random.Range(-spawnArea.x, spawnArea.x);
            float fixedY = 1.2f;

            spawnPos = new Vector2(randomX, fixedY);

            // 檢查距離是否符合要求
            if (player != null && Vector2.Distance(spawnPos, player.position) >= minSpawnDistance)
            {
                validPos = true;
            }
            attempts++;
        }

        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }
}