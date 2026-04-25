using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("=== 生成設定 ===")]
    public GameObject enemyPrefab;
    public Transform player;
    public float spawnInterval = 3f;
    public float minSpawnDistance = 5f;
    public Vector2 spawnArea = new Vector2(20f, 10f);

    [Header("=== 數量限制 ===")]
    public int maxEnemyCount = 5; // 限制最多同時存在幾個敵人

    private float timer;
    private List<GameObject> activeEnemies = new List<GameObject>();

    private void Update()
    {
        GameObject[] currentEnemies = GameObject.FindGameObjectsWithTag("Enemy");

        Debug.Log("當前場景中的敵人數量: " + currentEnemies.Length);

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
        Vector2 spawnPos;
        int attempts = 0;

        do
        {
            spawnPos = new Vector2(Random.Range(-spawnArea.x, spawnArea.x), Random.Range(-spawnArea.y, spawnArea.y));
            attempts++;
        } while (Vector2.Distance(spawnPos, player.position) < minSpawnDistance && attempts < 10);

        // 生成並加入清單
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        activeEnemies.Add(newEnemy);
    }
}