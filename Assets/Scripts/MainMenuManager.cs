using UnityEngine;
using UnityEngine.SceneManagement; // 用于场景管理

// 【新增】我们需要引入这个命名空间来使用编辑器专用的代码
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuManager : MonoBehaviour
{
    // StartGame 函数保持不变
    public void StartGame()
    {
        SceneManager.LoadScene("Example"); 
    }

    /// <summary>
    /// 【修改后】的退出游戏函数
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("请求退出游戏");

        // --- 使用条件编译指令 ---

        // 【如果】我们当前是在Unity编辑器环境中运行
        #if UNITY_EDITOR
            // 就执行这条编辑器专用的指令，来停止播放模式
            EditorApplication.isPlaying = false;
        
        // 【否则】（意味着这是在构建好的独立游戏包中运行）
        #else
            // 就执行真正的退出应用程序指令
            Application.Quit();
        #endif
    }
}