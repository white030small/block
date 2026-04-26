using UnityEngine;

public class Enemy : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            mainchar player = collision.gameObject.GetComponent<mainchar>();

            if (player != null)
            {
                // ★ 這行是關鍵，看看它到底是不是 true
                Debug.Log($"[偵錯] 撞到主角，主角目前 IsGroundPounding 狀態為: {player.isGroundPounding}");

                if (player.isGroundPounding)
                {
                    TakeDamage(1);
                }
            }
        }
    }

    public void TakeDamage(int damage)
    {
        Destroy(gameObject);
    }
}