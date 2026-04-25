using UnityEngine;

/// <summary>
/// 2D 攝影機跟隨腳本
/// 功能：平滑跟隨、前方偏移(Look Ahead)、Y軸死區、邊界限制
/// 使用方式：掛在主攝影機上，將角色拖入 Target 欄位
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    [Header("=== 目標 ===")]
    [SerializeField] private Transform target;

    [Header("=== 跟隨設定 ===")]
    [SerializeField] private float smoothSpeed = 8f;
    [SerializeField] private Vector2 offset = new Vector2(0f, 1.5f);

    [Header("=== 前方偏移 (Look Ahead) ===")]
    [Tooltip("角色面朝的方向會多看一點")]
    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private float lookAheadSpeed = 3f;

    [Header("=== Y 軸死區 ===")]
    [Tooltip("角色在此範圍內上下移動時，攝影機不跟隨Y軸")]
    [SerializeField] private float verticalDeadZone = 1f;

    [Header("=== 邊界限制（選用）===")]
    [SerializeField] private bool useBounds;
    [SerializeField] private float minX = -50f;
    [SerializeField] private float maxX = 50f;
    [SerializeField] private float minY = -10f;
    [SerializeField] private float maxY = 30f;

    private float currentLookAhead;
    private float targetLookAhead;
    private float lastTargetX;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("[CameraFollow2D] 未指定目標！請拖入 Target。");
            return;
        }

        // 初始定位
        lastTargetX = target.position.x;
        Vector3 startPos = target.position + (Vector3)offset;
        startPos.z = transform.position.z;
        transform.position = startPos;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // ---- Look Ahead ----
        float moveDir = target.position.x - lastTargetX;
        if (Mathf.Abs(moveDir) > 0.01f)
            targetLookAhead = Mathf.Sign(moveDir) * lookAheadDistance;

        currentLookAhead = Mathf.Lerp(currentLookAhead, targetLookAhead, lookAheadSpeed * Time.deltaTime);
        lastTargetX = target.position.x;

        // ---- 目標位置 ----
        float targetX = target.position.x + offset.x + currentLookAhead;
        float targetY = target.position.y + offset.y;

        // Y 軸死區：只有超出範圍才跟隨
        float currentY = transform.position.y;
        float deltaY = targetY - currentY;
        if (Mathf.Abs(deltaY) < verticalDeadZone)
            targetY = currentY;

        Vector3 desiredPos = new Vector3(targetX, targetY, transform.position.z);

        // ---- 平滑移動 ----
        Vector3 smoothedPos = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);

        // ---- 邊界限制 ----
        if (useBounds)
        {
            smoothedPos.x = Mathf.Clamp(smoothedPos.x, minX, maxX);
            smoothedPos.y = Mathf.Clamp(smoothedPos.y, minY, maxY);
        }

        transform.position = smoothedPos;
    }
}
