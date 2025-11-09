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
        
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0);
        
        
        _light.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}