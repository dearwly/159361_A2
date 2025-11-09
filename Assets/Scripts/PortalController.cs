using UnityEngine;
using UnityEngine.SceneManagement; 

public class PortalController : MonoBehaviour
{
    [Header("传送设置")]
    [Tooltip("将要加载的目标场景的文件名")]
    public string sceneToLoad; 
    private void OnTriggerEnter(Collider other)
    {
        
        if (other.CompareTag("Player"))
        {
            
            Debug.Log("玩家已进入传送门！准备传送...");

            
            LoadNextScene();
        }
    }

    private void LoadNextScene()
    {
        
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            
            Debug.LogError("Portal's 'Scene To Load' is not set! Please set it in the Inspector.");
        }
    }
}