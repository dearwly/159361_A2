using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("Player Stats")]
    [SerializeField] public float maxHealth = 100f;
    public float CurrentHealth { get; private set; }

    [SerializeField] private float maxStamina = 100f;
    public float CurrentStamina { get; private set; }

    [Header("Stamina Logic")]
    [SerializeField] private float staminaRegenRate = 10f;
    [SerializeField] private float staminaRegenDelay = 1.5f;

    [Header("Consumables")]
    public int maxFlasks = 5;
    public int currentFlasks;
    private float staminaRegenTimer;

    [Header("Power Up")]
    public float powerUpMultiplier = 1.5f;
    public float powerUpDuration = 30f;
    private float powerUpTimer;

    // --- 私有UI引用 ---
    private Slider healthSlider;
    private Slider staminaSlider;
    private TextMeshProUGUI flaskQuantityText;
    private GameObject powerUpIcon;

    // --- 事件委托 ---
    public delegate void HealthChangedDelegate(float newHealth, float maxHealth);
    public event HealthChangedDelegate OnHealthChanged;
    public delegate void StaminaChangedDelegate(float newStamina, float maxStamina);
    public event StaminaChangedDelegate OnStaminaChanged;

    private PlayerController playerController;
    public bool IsPoweredUp => powerUpTimer > 0;

    void Awake()
    {
        CurrentHealth = maxHealth;
        CurrentStamina = maxStamina;
        playerController = GetComponent<PlayerController>();

        // 自动寻找UI元素
        GameObject healthBarObject = GameObject.FindGameObjectWithTag("HealthBar");
        if (healthBarObject != null) healthSlider = healthBarObject.GetComponent<Slider>();

        GameObject staminaBarObject = GameObject.FindGameObjectWithTag("StaminaBar");
        if (staminaBarObject != null) staminaSlider = staminaBarObject.GetComponent<Slider>();
        
        GameObject flaskTextObject = GameObject.FindGameObjectWithTag("FlaskText");
        if (flaskTextObject != null) flaskQuantityText = flaskTextObject.GetComponent<TextMeshProUGUI>();

        GameObject powerUpIconObject = GameObject.FindGameObjectWithTag("PowerUpIcon");
        if (powerUpIconObject != null) powerUpIcon = powerUpIconObject;
        if (powerUpIcon != null) powerUpIcon.SetActive(false);
    }

    void Start()
    {
        currentFlasks = maxFlasks; 
        UpdateUI();
    }

    void Update()
    {
        HandleStaminaRegen();

        if (powerUpTimer > 0)
        {
            powerUpTimer -= Time.deltaTime;
            if (powerUpTimer <= 0)
            {
                powerUpTimer = 0;
                if (powerUpIcon != null) { powerUpIcon.SetActive(false); }
                Debug.Log("Power Up has expired.");
            }
        }
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        // 如果已经死了，就不再接受伤害
        if (playerController != null && playerController.isDead) return;

        if (playerController != null && !playerController.IsCastingSkill)
        {
            playerController.TriggerHitAnimation(hitNormal);
        }

        if (playerController != null && playerController.IsBlocking)
        {
            BlockDamage(damage);
        }
        else
        {
            CurrentHealth -= damage;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (CurrentHealth <= 0)
            {
                Die(hitNormal); // 将受击方向传递给死亡函数
            }
        }
    }

    private void BlockDamage(float damage)
    {
        float staminaCost = damage * 0.5f;
        ConsumeStamina(staminaCost);
        Debug.Log("Player blocked the attack!");
    }
    
    private void HandleStaminaRegen()
    {
        if (staminaRegenTimer > 0)
        {
            staminaRegenTimer -= Time.deltaTime;
        }
        else if (CurrentStamina < maxStamina)
        {
            CurrentStamina = Mathf.MoveTowards(CurrentStamina, maxStamina, staminaRegenRate * Time.deltaTime);
            OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
        }
    }

    public void AnimationEvent_ApplyPowerUp()
    {
        if (!IsPoweredUp) { Debug.Log("Power Up applied!"); }
        powerUpTimer = powerUpDuration;
        if (powerUpIcon != null) { powerUpIcon.SetActive(true); }
    }

    public void ConsumeStamina(float amount)
    {
        if (CurrentStamina >= amount)
        {
            CurrentStamina -= amount;
            staminaRegenTimer = staminaRegenDelay;
            OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
        }
    }

    public bool HasEnoughStamina(float amount)
    {
        return CurrentStamina >= amount;
    }
    
    private void UpdateFlaskUI()
    {
        if (flaskQuantityText != null)
        {
            flaskQuantityText.text = currentFlasks.ToString();
        }
    }

    public void AnimationEvent_HealAndConsumeFlask()
    {
        if (currentFlasks > 0)
        {
            currentFlasks--;
            UpdateFlaskUI();
            Heal(maxHealth / 2);
            Debug.Log("Flask consumed and heal applied!");
        }
    }

    public void Heal(float amount)
    {
        CurrentHealth += amount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
    
    /// <summary>
    /// 【核心修改】玩家死亡时调用的函数
    /// </summary>
    private void Die(Vector3 hitNormal)
    {
        // 检查玩家控制器是否已经标记为死亡，防止重复调用
        if (playerController != null && playerController.isDead) return;

        Debug.Log("Player has died.");
        
        // 触发玩家控制器中的死亡动画逻辑
        if (playerController != null)
        {
            playerController.TriggerDeathAnimation(hitNormal);
        }

        // ======[ 【核心新增】在这里通知GameManager ]======
        // 检查GameManager实例是否存在，然后调用OnPlayerDied函数
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDied();
        }
        else
        {
            Debug.LogWarning("GameManager.Instance not found. Cannot display Death Panel.");
        }
        // ===============================================
    }

    private void UpdateUI()
    {
        UpdateHealthUI(CurrentHealth, maxHealth);
        UpdateStaminaUI(CurrentStamina, maxStamina);
        UpdateFlaskUI();
    }
    
    private void OnEnable()
    {
        OnHealthChanged += UpdateHealthUI;
        OnStaminaChanged += UpdateStaminaUI;
    }

    private void OnDisable()
    {
        OnHealthChanged -= UpdateHealthUI;
        OnStaminaChanged -= UpdateStaminaUI;
    }

    private void UpdateHealthUI(float newHealth, float maxHealthValue)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealthValue;
            healthSlider.value = newHealth;
        }
    }

    private void UpdateStaminaUI(float newStamina, float maxStaminaValue)
    {
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStaminaValue;
            staminaSlider.value = newStamina;
        }
    }
}