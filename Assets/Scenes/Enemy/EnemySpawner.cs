using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("=== �ͦ��]�w ===")]
    public GameObject enemyPrefab;
    public Transform player;
    public float spawnInterval = 3f;
    public float minSpawnDistance = 5f;
    public Vector2 spawnArea = new Vector2(20f, 10f);

    [Header("=== �ƶq���� ===")]
    public int maxEnemyCount = 5; // ����̦h�P�ɦs�b�X�ӼĤH

    private float timer;
    private List<GameObject> activeEnemies = new List<GameObject>();

    private void Update()
    {
        GameObject[] currentEnemies = GameObject.FindGameObjectsWithTag("Enemy");

        //Debug.Log("���e���������ĤH�ƶq: " + currentEnemies.Length);

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

        // �ͦ��å[�J�M��
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        activeEnemies.Add(newEnemy);
    }
}