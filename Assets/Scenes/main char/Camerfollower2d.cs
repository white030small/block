using UnityEngine;

/// <summary>
/// 優化版 2D 攝影機跟隨腳本
/// 修正了會誤鎖主角位置的 Y 軸邏輯
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    [Header("=== 目標設定 ===")]
    [SerializeField] private Transform target;

    [Header("=== 跟隨設定 ===")]
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private Vector2 offset = new Vector2(0f, 1.5f);

    [Header("=== 前方偏移 (Look Ahead) ===")]
    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private float lookAheadSpeed = 3f;

    [Header("=== Y 軸死區 ===")]
    [SerializeField] private float verticalDeadZone = 1f;

    [Header("=== 邊界限制 ===")]
    [SerializeField] private bool useBounds;
    [SerializeField] private float minX = -50f, maxX = 50f;
    [SerializeField] private float minY = -10f, maxY = 30f;

    private Vector3 currentVelocity = Vector3.zero;
    private float currentLookAhead;
    private float targetLookAhead;
    private float lastTargetX;

    private void Start()
    {
        if (target != null) lastTargetX = target.position.x;
    }

    private void LateUpdate()
    {
        // 安全檢查：若沒有目標或目標就是攝影機自己，直接跳出，防止鎖定座標
        if (target == null || target == this.transform) return;

        // 1. Look Ahead 計算
        float moveDir = target.position.x - lastTargetX;
        if (Mathf.Abs(moveDir) > 0.01f)
            targetLookAhead = Mathf.Sign(moveDir) * lookAheadDistance;

        currentLookAhead = Mathf.Lerp(currentLookAhead, targetLookAhead, lookAheadSpeed * Time.deltaTime);
        lastTargetX = target.position.x;

        // 2. 計算目標位置
        float targetX = target.position.x + offset.x + currentLookAhead;
        float targetY = target.position.y + offset.y;

        // 3. Y 軸死區邏輯 (只改變攝影機的目標 Y，不影響主角)
        float cameraY = transform.position.y;
        if (Mathf.Abs(targetY - cameraY) <= verticalDeadZone)
        {
            targetY = cameraY;
        }

        Vector3 desiredPos = new Vector3(targetX, targetY, transform.position.z);

        // 4. 平滑移動
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref currentVelocity, smoothTime);

        // 5. 邊界限制
        if (useBounds)
        {
            transform.position = new Vector3(
                Mathf.Clamp(transform.position.x, minX, maxX),
                Mathf.Clamp(transform.position.y, minY, maxY),
                transform.position.z
            );
        }
    }
}