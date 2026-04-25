// 掛在敵人身上
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public void TakeDamage()
    {
        // 這裡寫死亡特效或銷毀敵人
        GetComponent<SpriteRenderer>().enabled = false;
    }
    private void Update()
    {
        // 保險：如果敵人因為 Bug 飛出地圖外，強制自我銷毀，避免占用數量限制
        if (Mathf.Abs(transform.position.x) > 100 || Mathf.Abs(transform.position.y) > 100)
        {
            Destroy(gameObject);
        }
    }
}