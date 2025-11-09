using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; 

    [Header("UI Prefabs")]
    [Tooltip("将你的死亡UI面板预制件拖到这里")]
    public GameObject deathPanelPrefab;
    private GameObject deathPanelInstance;

    void Awake()
    {
        
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

    
    
    
    public void OnPlayerDied()
    {
        
        if (deathPanelInstance == null && deathPanelPrefab != null)
        {
            
            Canvas currentCanvas = FindFirstObjectByType<Canvas>();
            if (currentCanvas == null) 
            {
                Debug.LogError("GameManager: Can't find a Canvas to spawn the Death Panel!");
                return;
            }
            deathPanelInstance = Instantiate(deathPanelPrefab, currentCanvas.transform);
            
            
            
            Button respawnButton = deathPanelInstance.transform.Find("RespawnButton")?.GetComponent<Button>();
            if (respawnButton != null)
            {
                respawnButton.onClick.AddListener(Respawn);
            }

            
            Button mainMenuButton = deathPanelInstance.transform.Find("MainMenuButton")?.GetComponent<Button>();
            if (mainMenuButton != null)
            {
                
                mainMenuButton.onClick.AddListener(SettingsManager.Instance.QuitToMainMenu);
            }
        }

        
        deathPanelInstance.SetActive(true);
        
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        
        Time.timeScale = 0.5f;
    }

    
    
    
    public void Respawn()
    {
        Debug.Log("Respawning...");

        
        Time.timeScale = 1f;
        
        
        if (deathPanelInstance != null)
        {
            deathPanelInstance.SetActive(false);
        }

        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}