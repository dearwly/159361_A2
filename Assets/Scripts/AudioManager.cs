using System.Collections; // 必须添加，用于使用协程(Coroutines)
using UnityEngine;
using UnityEngine.SceneManagement; // 必须添加，用于监听场景加载 (虽然本脚本未直接使用，但作为管理器最好保留)

// 确保游戏对象上有关联的AudioSource组件
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    // 创建一个静态的自身实例，方便其他任何脚本通过 AudioManager.Instance 来访问它
    public static AudioManager Instance;

    private AudioSource bgmSource; // 用于引用背景音乐的AudioSource组件
    private Coroutine fadeCoroutine; // 用于保存当前正在运行的淡入淡出协程的引用

    [Header("音乐设置")]
    [Tooltip("音乐切换时的淡入淡出效果的持续时间（秒）")]
    public float fadeDuration = 1.0f;

    /// <summary>
    /// Awake在脚本实例被加载时调用，在Start之前
    /// </summary>
    void Awake()
    {
        // --- 实现单例模式，确保全局只有一个AudioManager ---
        
        // 如果还没有实例存在
        if (Instance == null)
        {
            // 就将自己设为唯一的实例
            Instance = this;
            // 并且在加载新场景时不要销毁自己
            DontDestroyOnLoad(gameObject);
            
            // 获取自己身上的AudioSource组件，以便后续控制
            bgmSource = GetComponent<AudioSource>();
        }
        // 如果已经有一个实例存在了（例如，从一个场景返回主菜单时，主菜单又创建了一个新的）
        else
        {
            // 就销毁这个后来者，保证全局唯一性
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 【核心】切换背景音乐的公开方法。
    /// 其他脚本（如 BGMTrigger）将调用这个方法。
    /// </summary>
    /// <param name="newBGM">要切换到的新音乐 AudioClip 文件</param>
    public void SwitchBGM(AudioClip newBGM)
    {
        // 优化：如果要切换的音乐和当前正在播放的音乐是同一个，就什么都不做
        if (bgmSource.clip == newBGM && bgmSource.isPlaying)
        {
            return;
        }

        // 如果之前有正在进行的淡入淡出协程，先把它停掉，防止冲突
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // 启动一个新的淡入淡出协程，并保存它的引用
        fadeCoroutine = StartCoroutine(FadeAndSwitchBGM(newBGM));
    }

    /// <summary>
    /// 【核心】执行淡入淡出和音乐切换的协程 (Coroutine)
    /// </summary>
    private IEnumerator FadeAndSwitchBGM(AudioClip newBGM)
    {
        // 如果当前有音乐正在播放，则执行淡出
        if (bgmSource.isPlaying)
        {
            float startVolume = bgmSource.volume;
            float timer = 0f;

            // --- 阶段1: 淡出当前音乐 ---
            // 我们只用一半的fadeDuration时间来淡出
            while (timer < fadeDuration / 2)
            {
                // 使用 Mathf.Lerp 进行平滑的线性插值，计算当前帧的音量
                bgmSource.volume = Mathf.Lerp(startVolume, 0, timer / (fadeDuration / 2));
                // 使用 unscaledDeltaTime 确保在游戏暂停时（Time.timeScale = 0），淡出效果依然能正常进行
                timer += Time.unscaledDeltaTime; 
                yield return null; // 等待下一帧
            }
            bgmSource.volume = 0;
            bgmSource.Stop();
        }

        // --- 阶段2: 切换并淡入新音乐 ---
        bgmSource.clip = newBGM;
        
        // 如果传入的新音乐是null，就不播放，直接结束
        if (newBGM == null)
        {
            fadeCoroutine = null;
            yield break; // 提前退出协程
        }

        bgmSource.Play();
        
        // 重置计时器，准备淡入
        float timer_fadeIn = 0f;
        
        // 获取用户在设置菜单中保存的目标音量，如果没有就默认为1
        float targetVolume = PlayerPrefs.GetFloat("MasterVolume", 1f); 

        // 我们用另一半的fadeDuration时间来淡入
        while (timer_fadeIn < fadeDuration / 2)
        {
            bgmSource.volume = Mathf.Lerp(0, targetVolume, timer_fadeIn / (fadeDuration / 2));
            timer_fadeIn += Time.unscaledDeltaTime;
            yield return null;
        }
        // 确保最终音量精确地等于目标音量
        bgmSource.volume = targetVolume;

        // 协程任务完成，清空引用
        fadeCoroutine = null;
    }

    /// <summary>
    /// 一个公开的方法，方便其他脚本（如SettingsManager）直接设置BGM音量
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        if (bgmSource != null)
        {
            bgmSource.volume = volume;
        }
    }
}