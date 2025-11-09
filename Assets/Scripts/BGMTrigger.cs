using UnityEngine;

public class BGMTrigger : MonoBehaviour
{
    [Tooltip("将这个场景专属的背景音乐文件拖到这里")]
    public AudioClip sceneBGM;

    void Start()
    {
        
        if (AudioManager.Instance != null)
        {
            
            AudioManager.Instance.SwitchBGM(sceneBGM);
        }
        else
        {
            Debug.LogWarning("BGMTrigger: AudioManager not found in the scene!");
        }
    }
}