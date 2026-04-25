using UnityEngine;

/// <summary>
/// 掙脫進度條 - 顯示在敵人頭上
/// 主角被吸引時會出現，狂按按鍵填滿就能掙脫
/// 
/// 設定步驟：
/// 1. 在敵人底下建立一個子物件（空的 GameObject）
/// 2. 掛上此腳本 + SpriteRenderer
/// 3. SpriteRenderer 用一個純白色正方形 Sprite
/// 4. 調整位置到敵人頭上
/// 5. 將 SuctionEnemy 拖入 Enemy 欄位
/// </summary>
public class MashProgressBar : MonoBehaviour
{
    [SerializeField] private SuctionEnemy enemy;
    [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color fillColor = new Color(0f, 1f, 0.5f, 1f);
    [SerializeField] private float barWidth = 1.2f;
    [SerializeField] private float barHeight = 0.15f;

    private Transform fillBar;
    private SpriteRenderer bgRenderer;
    private SpriteRenderer fillRenderer;

    private void Start()
    {
        // 建立背景條
        bgRenderer = GetComponent<SpriteRenderer>();
        if (bgRenderer == null)
            bgRenderer = gameObject.AddComponent<SpriteRenderer>();

        bgRenderer.sprite = CreateSquareSprite();
        bgRenderer.color = backgroundColor;
        bgRenderer.sortingOrder = 100;
        transform.localScale = new Vector3(barWidth, barHeight, 1f);

        // 建立填充條（子物件）
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(transform);
        fillObj.transform.localPosition = Vector3.zero;
        fillObj.transform.localScale = new Vector3(1f, 1f, 1f);

        fillRenderer = fillObj.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = CreateSquareSprite();
        fillRenderer.color = fillColor;
        fillRenderer.sortingOrder = 101;
        fillBar = fillObj.transform;

        // 預設隱藏
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 更新進度條，progress 為 0~1
    /// </summary>
    public void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);

        if (progress > 0f && !gameObject.activeSelf)
            gameObject.SetActive(true);
        else if (progress <= 0f && gameObject.activeSelf)
            gameObject.SetActive(false);

        if (fillBar != null)
        {
            // 從左往右填滿
            fillBar.localScale = new Vector3(progress, 1f, 1f);
            fillBar.localPosition = new Vector3((progress - 1f) / 2f, 0f, 0f);
        }

        // 快掙脫時變色
        if (fillRenderer != null)
        {
            fillRenderer.color = progress > 0.7f
                ? Color.Lerp(fillColor, Color.yellow, (progress - 0.7f) / 0.3f)
                : fillColor;
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(4, 4);
        Color[] colors = new Color[16];
        for (int i = 0; i < 16; i++) colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }
}