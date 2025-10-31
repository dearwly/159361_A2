using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    private Light _light;

    [Tooltip("最小光照强度")]
    public float minIntensity = 0.8f;
    [Tooltip("最大光照强度")]
    public float maxIntensity = 1.2f;

    [Tooltip("闪烁速度")]
    public float flickerSpeed = 5.0f;

    private void Awake()
    {
        _light = GetComponent<Light>();
    }

    void Update()
    {
        // 使用 PerlinNoise 生成一个平滑的随机值，比完全随机更自然
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0);
        
        // 将 0-1 的 noise 值映射到我们的最小/最大强度之间
        _light.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}