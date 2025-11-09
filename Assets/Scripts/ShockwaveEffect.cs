using UnityEngine;
public class ShockwaveEffect : MonoBehaviour
{
    public Transform vfxMesh;       
    public float duration = 0.5f;   
    public Vector3 endScale = new Vector3(10, 10, 10); 

    private Vector3 initialScale;
    private float timer;

    void Awake()
    {
        if (vfxMesh != null)
        {
            initialScale = vfxMesh.localScale; 
        }
    }

    void OnEnable()
    {
        
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
            
            
            float progress = timer / duration;

            
            if (vfxMesh != null)
            {
                vfxMesh.localScale = Vector3.Lerp(initialScale, endScale, progress);
            }
        }
        else
        {
            
            
            Destroy(gameObject);
        }
    }
}