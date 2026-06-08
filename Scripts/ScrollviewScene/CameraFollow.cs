using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target;

    [Header("偏移量")]
    public Vector3 offset = new Vector3(0, 2, -10);

    [Header("平滑度")]
    public float smoothSpeed = 3f;

    [Header("摄像机边界（火车范围）")]
    public bool useBoundaries = true;
    public Transform leftBoundary;
    public Transform rightBoundary;

    private Camera cam;
    private float camHalfWidth;

    void Start()
    {
        cam = GetComponent<Camera>();
        camHalfWidth = cam.orthographicSize * cam.aspect;

        if (target != null)
            transform.position = target.position + offset;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + offset;
        desiredPos.z = offset.z;

        // 平滑移动
        Vector3 smoothedPos = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);

        // 限制摄像机不超出火车范围
        if (useBoundaries && leftBoundary != null && rightBoundary != null)
        {
            float minX = leftBoundary.position.x + camHalfWidth;
            float maxX = rightBoundary.position.x - camHalfWidth;
            smoothedPos.x = Mathf.Clamp(smoothedPos.x, minX, maxX);
        }

        transform.position = smoothedPos;
    }
}