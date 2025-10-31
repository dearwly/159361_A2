using UnityEngine;
public class ShockwaveEffect : MonoBehaviour
{
    public Transform vfxMesh;       // 我们的视觉模型
    public float duration = 0.5f;   // 特效持续时间
    public Vector3 endScale = new Vector3(10, 10, 10); // 最终要放大到的尺寸

    private Vector3 initialScale;
    private float timer;

    void Awake()
    {
        if (vfxMesh != null)
        {
            initialScale = vfxMesh.localScale; // 记录初始大小
        }
    }

    void OnEnable()
    {
        // 每次激活时，重置计时器和大小
        timer = 0f;
        if (vfxMesh != null)
        {
            vfxMesh.localScale = initialScale;
        }
    }

    void Update()
    {
        if (timer < duration)
        {
            timer += Time.deltaTime;
            
            // 计算当前进度 (0 到 1)
            float progress = timer / duration;

            // 使用 Lerp (线性插值) 平滑地放大 vfxMesh
            if (vfxMesh != null)
            {
                vfxMesh.localScale = Vector3.Lerp(initialScale, endScale, progress);
            }
        }
        else
        {
            // 持续时间结束，销毁或隐藏特效对象
            // 如果你使用的是对象池，这里应该是隐藏；否则直接销毁。
            Destroy(gameObject);
        }
    }
}