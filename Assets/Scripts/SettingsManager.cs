using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;
    public GameObject settingsPanelPrefab;

    private GameObject settingsPanelInstance;
    private bool isPaused = false;

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

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                CloseSettings();
            }
            else
            {
                if (SceneManager.GetActiveScene().name != "MainMenu")
                {
                    OpenSettings();
                }
            }
        }
    }

    public void OpenSettings()
    {
        if (settingsPanelInstance == null)
        {
            Canvas currentCanvas = FindObjectOfType<Canvas>();
            if (currentCanvas == null)
            {
                Debug.LogError("SettingsManager 错误: 在当前场景中找不到任何Canvas!");
                return;
            }
            settingsPanelInstance = Instantiate(settingsPanelPrefab, currentCanvas.transform);
            RebindUIElements();
        }

        isPaused = true;
        settingsPanelInstance.SetActive(true);
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseSettings()
    {
        if (settingsPanelInstance != null)
        {
            isPaused = false;
            settingsPanelInstance.SetActive(false);
            Time.timeScale = 1f;

            if (SceneManager.GetActiveScene().name != "MainMenu")
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    public void QuitToMainMenu()
    {
        Debug.Log("QuitToMainMenu() 函数被成功调用！");

        
        Time.timeScale = 1f;
        isPaused = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            
            
            
            CloseSettings();
        }
        else
        {
            
            if (settingsPanelInstance != null)
            {
                Destroy(settingsPanelInstance);
                settingsPanelInstance = null;
            }
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void SetVolume(float volume)
{
    
    AudioListener.volume = volume; 
    
    
    if (AudioManager.Instance != null)
    {
        AudioManager.Instance.SetBGMVolume(volume);
    }

    
    PlayerPrefs.SetFloat("MasterVolume", volume);
}
    
    
    private void RebindUIElements()
    {
        if (settingsPanelInstance == null) return;
        
        Button backButton = settingsPanelInstance.transform.Find("Back")?.GetComponent<Button>(); 
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(CloseSettings);
            Debug.Log("成功找到并绑定 'Back' 按钮！");
        }
        else
        {
            Debug.LogError("SettingsManager 错误: 在SettingsPanel预制件中找不到名为 'Back' 的按钮！");
        }
        
        Button quitButton = settingsPanelInstance.transform.Find("QuitButton")?.GetComponent<Button>();
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitToMainMenu);
            Debug.Log("成功找到并绑定 'QuitButton' 按钮！");
        }
        else
        {
            Debug.LogError("SettingsManager 错误: 在SettingsPanel预制件中找不到名为 'QuitButton' 的按钮！");
        }
        
        Slider volumeSlider = settingsPanelInstance.GetComponentInChildren<Slider>();
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveAllListeners();
            float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            volumeSlider.value = savedVolume;
            SetVolume(savedVolume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
            Debug.Log("成功找到并绑定音量滑块！");
        }
        else
        {
            Debug.LogWarning("SettingsManager 警告: 在SettingsPanel预制件中找不到任何Slider组件。");
        }
    }
}