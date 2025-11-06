using UnityEngine;
using UnityEngine.SceneManagement; // 必须添加，用于场景管理！

public class PortalController : MonoBehaviour
{
    [Header("传送设置")]
    [Tooltip("将要加载的目标场景的文件名")]
    public string sceneToLoad; // 在检视窗口中设置要加载的场景名称

    /// <summary>
    /// 当有其他碰撞体进入这个触发器时，这个函数会自动被调用
    /// </summary>
    /// <param name="other">进入触发器的那个物体的碰撞体</param>
    private void OnTriggerEnter(Collider other)
    {
        // 检查：进入触发器的物体，它的标签是不是 "Player"？
        if (other.CompareTag("Player"))
        {
            // 如果是玩家，就在控制台打印一条消息，方便我们调试
            Debug.Log("玩家已进入传送门！准备传送...");

            // 开始加载我们指定的新场景
            LoadNextScene();
        }
    }

    private void LoadNextScene()
    {
        // 检查我们是否设置了一个有效的场景名
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            // 使用场景管理器来加载场景
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            // 如果忘了在检视窗口设置场景名，就给出错误提示
            Debug.LogError("Portal's 'Scene To Load' is not set! Please set it in the Inspector.");
        }
    }
}