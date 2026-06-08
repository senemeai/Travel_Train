using UnityEngine;

public class TrainPlayerController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;

    [Header("火车边界")]
    public Transform trainLeftBoundary;   // 火车左边界
    public Transform trainRightBoundary;  // 火车右边界

    [Header("动画")]
    public bool useAnimator = true;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private float horizontalInput;
    private bool facingRight = true;
    private float minX, maxX;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 计算移动范围
        if (trainLeftBoundary != null && trainRightBoundary != null)
        {
            float playerHalfWidth = GetComponent<SpriteRenderer>().bounds.extents.x;
            minX = trainLeftBoundary.position.x + playerHalfWidth;
            maxX = trainRightBoundary.position.x - playerHalfWidth;
        }
    }

    void Update()
    {
        HandleInput();
        HandleAnimation();
    }

    void HandleInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        Vector3 newPos = transform.position;
        newPos.x += horizontalInput * moveSpeed * Time.deltaTime;

        // 限制在火车范围内
        if (trainLeftBoundary != null && trainRightBoundary != null)
        {
            newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
        }

        transform.position = newPos;

        // 更新朝向
        if (horizontalInput > 0) facingRight = true;
        else if (horizontalInput < 0) facingRight = false;

        if (spriteRenderer != null)
            spriteRenderer.flipX = !facingRight;
    }

    void HandleAnimation()
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(horizontalInput));
        }
    }
}