using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class mainchar : MonoBehaviour
{
    [Header("=== 設定 ===")]
    [SerializeField] private float cubeSize = 1f;
    [SerializeField] private float rollDuration = 0.2f;
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float groundPoundSpeed = 25f;

    private Rigidbody2D rb;
    private bool isRolling = false;
    private bool isGroundPounding = false;

    // 改用計數器，精確紀錄接觸物體數量
    private int contactCount = 0;
    private bool isGrounded => contactCount > 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }

    // 當觸碰到地面時增加計數
    private void OnCollisionEnter2D(Collision2D collision) { contactCount++; }
    // 當離開地面時減少計數
    private void OnCollisionExit2D(Collision2D collision) { contactCount--; }

    private void Update()
    {
        // 1. 翻滾 (加上 && isGrounded 檢查)
        if (!isRolling && !isGroundPounding && isGrounded)
        {
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            if (horizontalInput > 0.1f) StartCoroutine(RollCube(1));
            else if (horizontalInput < -0.1f) StartCoroutine(RollCube(-1));
        }

        // 2. 跳躍
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isRolling)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        // 3. 下壓
        if (Input.GetMouseButtonDown(0) && !isGrounded && !isGroundPounding)
        {
            StartCoroutine(GroundPound());
        }
    }

    // 翻滾協程維持不變...
    private IEnumerator RollCube(int direction)
    {
        isRolling = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        float half = cubeSize / 2f;
        Vector3 pivot = transform.position + new Vector3(direction * half, -half, 0f);

        float elapsed = 0f;
        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;
            transform.RotateAround(pivot, Vector3.forward, -90 * direction * (Time.deltaTime / rollDuration));
            yield return null;
        }

        transform.rotation = Quaternion.Euler(0, 0, Mathf.Round(transform.eulerAngles.z / 90f) * 90f);
        rb.bodyType = RigidbodyType2D.Dynamic;
        isRolling = false;
    }

    private IEnumerator GroundPound()
    {
        isGroundPounding = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0;
        yield return new WaitForSeconds(0.1f);

        rb.gravityScale = 4;
        rb.linearVelocity = new Vector2(0, -groundPoundSpeed);

        yield return new WaitUntil(() => isGrounded);
        isGroundPounding = false;
    }
}