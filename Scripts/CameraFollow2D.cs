using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target;
    
    [Header("跟随设置")]
    public Vector3 offset = new Vector3(0, 0, -10);
    public float smoothSpeed = 5f;
    
    [Header("边界限制")]
    public bool useBoundaries = false;
    public float minX = -10f;
    public float maxX = 10f;
    public float minY = -10f;
    public float maxY = 10f;

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;
        
        // 平滑跟随
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        
        // 边界限制
        if (useBoundaries)
        {
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minX, maxX);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minY, maxY);
        }

        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, offset.z);
    }
}