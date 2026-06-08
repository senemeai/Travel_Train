using UnityEngine;

public class Player2DMovement : MonoBehaviour
{
    [Header("移动速度")]
    public float moveSpeed = 5f;

    [Header("边界限制")]
    public bool useBoundaries = false;
    public float minX = -10f;
    public float maxX = 10f;

    private Rigidbody2D rb;
    private float horizontalInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    void Update()
    {
        // 获取输入
        horizontalInput = Input.GetAxisRaw("Horizontal");
    }

    void FixedUpdate()
    {
        // 移动玩家 - 使用velocity代替linearVelocity
        Vector2 currentVelocity = rb.velocity;
        currentVelocity.x = horizontalInput * moveSpeed;
        rb.velocity = currentVelocity;

        // 可选：边界限制
        if (useBoundaries)
        {
            Vector3 clampedPosition = transform.position;
            clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);
            transform.position = clampedPosition;
        }
    }
}