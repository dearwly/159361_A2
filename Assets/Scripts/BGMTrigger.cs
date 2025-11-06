using UnityEngine;

public class BGMTrigger : MonoBehaviour
{
    [Tooltip("将这个场景专属的背景音乐文件拖到这里")]
    public AudioClip sceneBGM;

    void Start()
    {
        // 检查AudioManager是否存在
        if (AudioManager.Instance != null)
        {
            // 调用AudioManager的切换音乐方法
            AudioManager.Instance.SwitchBGM(sceneBGM);
        }
        else
        {
            Debug.LogWarning("BGMTrigger: AudioManager not found in the scene!");
        }
    }
}