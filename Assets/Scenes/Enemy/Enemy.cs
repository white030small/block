// 掛在敵人身上
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public void TakeDamage()
    {
        // 這裡寫死亡特效或銷毀敵人
        Destroy(gameObject);
    }
}