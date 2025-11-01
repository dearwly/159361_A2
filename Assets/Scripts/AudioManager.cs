using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // 创建一个静态的自身实例，方便其他脚本访问
    public static AudioManager Instance;

    private AudioSource bgmSource; // 用于引用背景音乐的AudioSource

    void Awake()
    {
        // --- 单例模式实现 ---
        // 如果还没有实例存在
        if (Instance == null)
        {
            // 就将自己设为实例
            Instance = this;
            // 并且在加载新场景时不销毁自己
            DontDestroyOnLoad(gameObject);
            
            // 获取自己身上的AudioSource组件
            bgmSource = GetComponent<AudioSource>();
        }
        // 如果已经有一个实例存在了（比如从主菜单回到了主菜单）
        else
        {
            // 就销毁这个后来者，保证唯一性
            Destroy(gameObject);
        }
    }
    
    // 我们可以创建一个公开的方法，方便之后用代码来改变BGM的音量
    public void SetBGMVolume(float volume)
    {
        if (bgmSource != null)
        {
            bgmSource.volume = volume;
        }
    }
}