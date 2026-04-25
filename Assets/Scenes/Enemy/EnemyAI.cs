using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public float speed = 3f;
    private Transform player;

    private void Start()
    {
        // 自動抓取標籤為 "Player" 的物件
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    private void Update()
    {
        if (player == null) return;

        // 計算方向
        Vector2 direction = (player.position - transform.position).normalized;

        // 根據方向移動
        transform.position += (Vector3)direction * speed * Time.deltaTime;

        // 簡單的面向朝向
        if (direction.x != 0)
            transform.localScale = new Vector3(Mathf.Sign(direction.x), 1, 1);
    }
}