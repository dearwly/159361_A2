using UnityEngine;
using UnityEngine.SceneManagement; 


#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuManager : MonoBehaviour
{
    
    public void StartGame()
    {
        SceneManager.LoadScene("Example"); 
    }

    
    
    
    public void QuitGame()
    {
        Debug.Log("请求退出游戏");
        #if UNITY_EDITOR
            
            EditorApplication.isPlaying = false;
        
        #else
            
            Application.Quit();
        #endif
    }
}