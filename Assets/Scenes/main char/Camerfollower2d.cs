using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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

    [Header("=== Boss 切換設定 ===")]
    public float bossOrthographicSize = 10f; // Boss 體型巨大，需要拉遠
    public float transitionSpeed = 0.05f;    // 放大縮小的過渡速度

    private float targetSize = 5f;           // 預設縮放
    private Camera cam;

    private Vector3 currentVelocity = Vector3.zero;
    private float currentLookAhead;
    private float targetLookAhead;
    private float lastTargetX;

    private void Start()
    {
        if (target != null) lastTargetX = target.position.x;
    }
    private void Awake()
    {
        cam = GetComponent<Camera>();
        targetSize = cam.orthographicSize;
    }

    // --- 新增：切換目標的方法 ---
    private bool isBossMode = false; // 新增狀態開關

    public void SwitchTarget(Transform newTarget, float newSize)
    {
        target = newTarget;
        targetSize = newSize; // 必須更新這個變數，LateUpdate 才會去 Lerp 它
        isBossMode = true;
        useBounds = false;
        Debug.Log($"[CameraFollow2D] 目標已更換為: {newTarget.name}, 目標大小: {targetSize}");
    }

    private System.Collections.IEnumerator SmoothSizeChange(float targetSize)
    {
        Camera cam = GetComponent<Camera>();
        float startSize = cam.orthographicSize;
        float duration = 2.0f; // 變焦時間
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cam.orthographicSize = Mathf.Lerp(startSize, targetSize, elapsed / duration);
            yield return null;
        }
    }
    private void LateUpdate()
    {
        // 1. 安全檢查
        if (target == null || target == this.transform) return;

        // 2. Boss 模式：極致簡單的跟隨，不計算 LookAhead 或偏移
        if (isBossMode)
        {
            Vector3 bossPos = new Vector3(target.position.x, target.position.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, bossPos, ref currentVelocity, smoothTime);

            // 隨時更新視野大小 (確保平滑過渡)
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, transitionSpeed);
            return; // 這裡直接退出，確保 Boss 模式下不受其他邏輯干擾
        }

        // --- 以下為原本的主角跟隨邏輯 (只有在非 Boss 模式下執行) ---

        // Look Ahead 計算
        float moveDir = target.position.x - lastTargetX;
        if (Mathf.Abs(moveDir) > 0.01f)
            targetLookAhead = Mathf.Sign(moveDir) * lookAheadDistance;

        currentLookAhead = Mathf.Lerp(currentLookAhead, targetLookAhead, lookAheadSpeed * Time.deltaTime);
        lastTargetX = target.position.x;

        // 目標位置計算
        float targetX = target.position.x + offset.x + currentLookAhead;
        float targetY = target.position.y + offset.y;

        // Y 軸死區邏輯
        float cameraY = transform.position.y;
        if (Mathf.Abs(targetY - cameraY) <= verticalDeadZone)
        {
            targetY = cameraY;
        }

        Vector3 desiredPos = new Vector3(targetX, targetY, transform.position.z);

        // 平滑移動
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref currentVelocity, smoothTime);

        // 邊界限制
        if (useBounds)
        {
            transform.position = new Vector3(
                Mathf.Clamp(transform.position.x, minX, maxX),
                Mathf.Clamp(transform.position.y, minY, maxY),
                transform.position.z
            );
        }

        // 確保非 Boss 模式下視野維持正常
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, transitionSpeed);
    }
}