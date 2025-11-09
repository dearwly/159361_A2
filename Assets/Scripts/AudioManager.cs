using System.Collections; 
using UnityEngine;
using UnityEngine.SceneManagement; 


[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    
    public static AudioManager Instance;

    private AudioSource bgmSource; 
    private Coroutine fadeCoroutine; 

    [Header("音乐设置")]
    [Tooltip("音乐切换时的淡入淡出效果的持续时间（秒）")]
    public float fadeDuration = 1.0f;

    
    
    
    void Awake()
    {
        
        
        
        if (Instance == null)
        {
            
            Instance = this;
            
            DontDestroyOnLoad(gameObject);
            
            
            bgmSource = GetComponent<AudioSource>();
        }
        
        else
        {
            
            Destroy(gameObject);
        }
    }

    
    
    
    
    
    public void SwitchBGM(AudioClip newBGM)
    {
        
        if (bgmSource.clip == newBGM && bgmSource.isPlaying)
        {
            return;
        }

        
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        
        fadeCoroutine = StartCoroutine(FadeAndSwitchBGM(newBGM));
    }

    
    
    
    private IEnumerator FadeAndSwitchBGM(AudioClip newBGM)
    {
        
        if (bgmSource.isPlaying)
        {
            float startVolume = bgmSource.volume;
            float timer = 0f;

            
            
            while (timer < fadeDuration / 2)
            {
                
                bgmSource.volume = Mathf.Lerp(startVolume, 0, timer / (fadeDuration / 2));
                
                timer += Time.unscaledDeltaTime; 
                yield return null; 
            }
            bgmSource.volume = 0;
            bgmSource.Stop();
        }

        
        bgmSource.clip = newBGM;
        
        
        if (newBGM == null)
        {
            fadeCoroutine = null;
            yield break; 
        }

        bgmSource.Play();
        
        
        float timer_fadeIn = 0f;
        
        
        float targetVolume = PlayerPrefs.GetFloat("MasterVolume", 1f); 

        
        while (timer_fadeIn < fadeDuration / 2)
        {
            bgmSource.volume = Mathf.Lerp(0, targetVolume, timer_fadeIn / (fadeDuration / 2));
            timer_fadeIn += Time.unscaledDeltaTime;
            yield return null;
        }
        
        bgmSource.volume = targetVolume;

        
        fadeCoroutine = null;
    }

    
    
    
    public void SetBGMVolume(float volume)
    {
        if (bgmSource != null)
        {
            bgmSource.volume = volume;
        }
    }
}