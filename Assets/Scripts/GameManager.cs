using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // 必须添加，用于操作Button

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // 单例模式

    [Header("UI Prefabs")]
    [Tooltip("将你的死亡UI面板预制件拖到这里")]
    public GameObject deathPanelPrefab;
    private GameObject deathPanelInstance;

    void Awake()
    {
        // --- 单例模式实现 ---
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 当玩家死亡时，由玩家脚本调用的公开函数
    /// </summary>
    public void OnPlayerDied()
    {
        // 如果死亡面板实例还不存在，就创建它
        if (deathPanelInstance == null && deathPanelPrefab != null)
        {
            // 在当前场景的Canvas下创建
            Canvas currentCanvas = FindFirstObjectByType<Canvas>();
            if (currentCanvas == null) 
            {
                Debug.LogError("GameManager: Can't find a Canvas to spawn the Death Panel!");
                return;
            }
            deathPanelInstance = Instantiate(deathPanelPrefab, currentCanvas.transform);
            
            // --- 自动为按钮绑定功能 ---
            // 查找复活按钮并绑定Respawn函数
            Button respawnButton = deathPanelInstance.transform.Find("RespawnButton")?.GetComponent<Button>();
            if (respawnButton != null)
            {
                respawnButton.onClick.AddListener(Respawn);
            }

            // 查找返回主菜单按钮并绑定GoToMainMenu函数
            Button mainMenuButton = deathPanelInstance.transform.Find("MainMenuButton")?.GetComponent<Button>();
            if (mainMenuButton != null)
            {
                // 复用SettingsManager中的功能！
                mainMenuButton.onClick.AddListener(SettingsManager.Instance.QuitToMainMenu);
            }
        }

        // 显示死亡面板
        deathPanelInstance.SetActive(true);
        
        // 解锁并显示鼠标，以便点击按钮
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // （可选）轻微减慢时间，营造死亡效果
        Time.timeScale = 0.5f;
    }

    /// <summary>
    // “复活”按钮的功能：重新加载当前关卡
    /// </summary>
    public void Respawn()
    {
        Debug.Log("Respawning...");

        // 恢复时间
        Time.timeScale = 1f;
        
        // 隐藏死亡面板（可选，因为场景会重载）
        if (deathPanelInstance != null)
        {
            deathPanelInstance.SetActive(false);
        }

        // 获取当前激活的场景，并重新加载它
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}