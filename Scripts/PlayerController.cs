using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("移动速度")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;

    [Header("跳跃设置")]
    public float jumpHeight = 3f;
    public float jumpDuration = 1.5f;

    // 组件
    private Animator animator;
    private Rigidbody2D rb;

    // 状态
    private bool facingRight = true;
    private bool isCrouching = false;
    private bool isRunning = false;
    private bool isJumping = false;
    private float jumpTimer = 0f;
    private float startY;

    // 记录起跳时的水平速度
    private float jumpStartSpeed = 0f;

    // 输入
    private float horizontalInput = 0f;

    // 动画哈希值
    private int speedHash = Animator.StringToHash("Speed");
    private int isCrouchingHash = Animator.StringToHash("IsCrouching");
    private int isRunningHash = Animator.StringToHash("IsRunning");
    private int facingRightHash = Animator.StringToHash("FacingRight");
    private int jumpHash = Animator.StringToHash("Jump");
    private int isJumpingHash = Animator.StringToHash("IsJumping");

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        startY = transform.position.y;

        // 关键设置：消除物理阻尼
        rb.drag = 0f;
        rb.angularDrag = 0f;
    }

    void Update()
    {
        HandleInput();
        HandleJump();
        UpdateAnimator();
    }

    void HandleInput()
    {
        horizontalInput = 0f;

        isCrouching = Input.GetKey(KeyCode.S);
        isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (!isCrouching && !isJumping)
        {
            bool pressingD = Input.GetKey(KeyCode.D);
            bool pressingA = Input.GetKey(KeyCode.A);

            if (pressingD)
            {
                horizontalInput = 1f;
                facingRight = true;
            }
            else if (pressingA)
            {
                horizontalInput = -1f;
                facingRight = false;
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.D))
                {
                    facingRight = true;
                }
                else if (Input.GetKeyDown(KeyCode.A))
                {
                    facingRight = false;
                }
            }
        }

        // 跳跃
        if (Input.GetButtonDown("Jump") && !isJumping && !isCrouching)
        {
            StartJump();
        }

        // 移动 - 使用 velocity 而不是 MovePosition
        if (!isJumping)
        {
            float currentSpeed = isRunning ? runSpeed : walkSpeed;
            rb.velocity = new Vector2(horizontalInput * currentSpeed, rb.velocity.y);
        }
    }
    private float startX;  // 添加在变量声明区域
    void StartJump()
    {
        isJumping = true;
        jumpTimer = 0f;
        startX = transform.position.x;  // 记录起始X位置
        startY = transform.position.y;

        // 记录起跳时的水平速度
        jumpStartSpeed = horizontalInput * (isRunning ? runSpeed : walkSpeed);

        // 更新朝向
        if (horizontalInput > 0)
            facingRight = true;
        else if (horizontalInput < 0)
            facingRight = false;

        // 先设置IsJumping，再触发Jump
        animator.SetBool(isJumpingHash, true);
        animator.SetTrigger(jumpHash);
    }

    void HandleJump()
    {
        if (isJumping)
        {
            jumpTimer += Time.deltaTime;
            float progress = jumpTimer / jumpDuration;

            if (progress < 1f)
            {
                float height = jumpHeight * 4 * progress * (1 - progress);

                // 让跳跃时的水平速度更快（比如1.5倍）
                float jumpHorizontalSpeed = jumpStartSpeed * 3.0f;  // 增加50%速度
                float horizontalMove = jumpHorizontalSpeed * Time.deltaTime;

                Vector2 pos = rb.position;
                pos.y = startY + height;
                pos.x += horizontalMove;
                rb.MovePosition(pos);
            }
            else
            {
                // 落地
                Vector2 pos = rb.position;
                pos.y = startY;
                rb.MovePosition(pos);
                isJumping = false;
                animator.SetBool(isJumpingHash, false);
            }
        }
    }

    void UpdateAnimator()
    {
        // 跳跃期间不更新Speed，避免打断Jump动画
        if (!isJumping)
        {
            if (horizontalInput > 0)
            {
                animator.SetFloat(speedHash, isRunning ? 2f : 1f);
            }
            else if (horizontalInput < 0)
            {
                animator.SetFloat(speedHash, isRunning ? -2f : -1f);
            }
            else
            {
                animator.SetFloat(speedHash, 0f);
            }
        }

        animator.SetBool(isCrouchingHash, isCrouching);
        animator.SetBool(isRunningHash, isRunning);
        animator.SetBool(facingRightHash, facingRight);
    }
}