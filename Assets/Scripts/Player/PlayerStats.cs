using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("Player Stats")]
    [SerializeField] public float maxHealth = 100f;
    public float CurrentHealth { get; private set; } // 使用属性来封装，外部只能读取，不能修改

    [SerializeField] private float maxStamina = 100f;
    public float CurrentStamina { get; private set; } // 同上

    [Header("Stamina Logic")]
    [SerializeField] private float staminaRegenRate = 10f;
    [SerializeField] private float staminaRegenDelay = 1.5f;

    [Header("Consumables")]
    public int maxFlasks = 5; // 最大血瓶数量
    public int currentFlasks; // 当前血瓶数量
    private float staminaRegenTimer;

    [Header("Power Up")]
    public float powerUpMultiplier = 1.5f; // 攻击力倍率
    public float powerUpDuration = 30f; // 持续时间
    private float powerUpTimer; // 计时器


    private Slider healthSlider;
    private Slider staminaSlider;
    private TextMeshProUGUI flaskQuantityText;
    private GameObject powerUpIcon;

    // ======[ 新增：事件委托，用于通知其他系统 ]======
    // 当生命值变化时触发
    public delegate void HealthChangedDelegate(float newHealth, float maxHealth);
    public event HealthChangedDelegate OnHealthChanged;
    // 当耐力值变化时触发
    public delegate void StaminaChangedDelegate(float newStamina, float maxStamina);
    public event StaminaChangedDelegate OnStaminaChanged;
    private PlayerController playerController;
    // 一个属性来判断当前是否处于强化状态
    public bool IsPoweredUp => powerUpTimer > 0;

    void Awake()
    {
        CurrentHealth = maxHealth;
        CurrentStamina = maxStamina;
        playerController = GetComponent<PlayerController>();

        // ======[ 新增：在 Awake 中自动寻找 UI 元素 ]======
        // 通过 Tag 寻找，这比通过名字更可靠
        GameObject healthBarObject = GameObject.FindGameObjectWithTag("HealthBar");
        if (healthBarObject != null) healthSlider = healthBarObject.GetComponent<Slider>();

        GameObject staminaBarObject = GameObject.FindGameObjectWithTag("StaminaBar");
        if (staminaBarObject != null) staminaSlider = staminaBarObject.GetComponent<Slider>();
        
        GameObject flaskTextObject = GameObject.FindGameObjectWithTag("FlaskText");
        if (flaskTextObject != null) flaskQuantityText = flaskTextObject.GetComponent<TextMeshProUGUI>();

        GameObject powerUpIconObject = GameObject.FindGameObjectWithTag("PowerUpIcon");
        if (powerUpIconObject != null) powerUpIcon = powerUpIconObject;

        // 找到后，立即隐藏 PowerUp 图标
        if (powerUpIcon != null) powerUpIcon.SetActive(false);
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        playerController = GetComponent<PlayerController>();
        if (playerController != null && !playerController.IsCastingSkill)
        {
            playerController.TriggerHitAnimation(hitNormal);
        }

        // 检查玩家是否正在格挡
        if (playerController != null && playerController.IsBlocking)
        {
            BlockDamage(damage);
        }
        else
        {
            // 正常受伤逻辑
            CurrentHealth -= damage;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0)
        {
            Die(hitNormal); // 将方向信息传递过去
        }
        }
    }

    private void BlockDamage(float damage)
    {
        // 格挡时，不掉血，而是消耗耐力
        // 这里的消耗值可以根据伤害值来计算，比如伤害的一半
        float staminaCost = damage * 0.5f;
        ConsumeStamina(staminaCost);

        // TODO: 在这里播放格挡成功的音效或特效
        Debug.Log("Player blocked the attack!");
    }

    void Start()
    {
        // 在 Start 中更新UI，确保UI引用已经准备好
        currentFlasks = maxFlasks; 
        UpdateUI();
    }

    void Update()
    {
        HandleStaminaRegen();

        // ======[ 新增：Power Up 计时器逻辑 ]======
        if (powerUpTimer > 0)
        {
            powerUpTimer -= Time.deltaTime;
            if (powerUpTimer <= 0)
            {
                // 计时结束，关闭效果
                powerUpTimer = 0;
                if (powerUpIcon != null)
                {
                    powerUpIcon.SetActive(false);
                }
                Debug.Log("Power Up has expired.");
            }
        }
    }

    // 将耐力恢复逻辑封装成一个独立的方法，让 Update 更整洁
    private void HandleStaminaRegen()
    {
        if (staminaRegenTimer > 0)
        {
            staminaRegenTimer -= Time.deltaTime;
        }
        else
        {
            if (CurrentStamina < maxStamina)
            {
                // 使用 Mathf.MoveTowards 来恢复，可以避免超过最大值，代码更简洁
                CurrentStamina = Mathf.MoveTowards(CurrentStamina, maxStamina, staminaRegenRate * Time.deltaTime);
                OnStaminaChanged?.Invoke(CurrentStamina, maxStamina); // 触发事件
            }
        }
    }

     // ======[ 新增：给动画事件调用的方法 ]======
    public void AnimationEvent_ApplyPowerUp()
    {
        // 如果不在强化状态，则开始强化
        if (!IsPoweredUp)
        {
            Debug.Log("Power Up applied!");
        }
        
        // 无论如何都重置计时器（重复使用会刷新时间）
        powerUpTimer = powerUpDuration;

        // 显示UI图标
        if (powerUpIcon != null)
        {
            powerUpIcon.SetActive(true);
        }
    }

    // 公开的消耗耐力方法
    public void ConsumeStamina(float amount)
    {
        if (CurrentStamina >= amount)
        {
            CurrentStamina -= amount;
            staminaRegenTimer = staminaRegenDelay;
            OnStaminaChanged?.Invoke(CurrentStamina, maxStamina); // 触发事件
        }
    }

    // 公开的检查耐力方法
    public bool HasEnoughStamina(float amount)
    {
        return CurrentStamina >= amount;
    }
    

        // 新增：专门更新血瓶UI的方法
    private void UpdateFlaskUI()
    {
        if (flaskQuantityText != null)
        {
            flaskQuantityText.text = currentFlasks.ToString();
        }
    }

    public void AnimationEvent_HealAndConsumeFlask()
    {
        // 只有在还有瓶子的情况下才执行（这是一个安全检查）
        if (currentFlasks > 0)
        {
            // 1. 消耗一个瓶子
            currentFlasks--;
            UpdateFlaskUI();

            // 2. 恢复生命值
            Heal(maxHealth / 2);

            Debug.Log("Flask consumed and heal applied by animation event!");
        }
    }

    public void Heal(float amount)
    {
        CurrentHealth += amount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth); // 触发事件
    }
    
    private void Die(Vector3 hitNormal)
    {
        // 检查是否已经调用过死亡逻辑
        if (playerController != null && playerController.isDead) return;

        Debug.Log("Player has died.");
        
        // 触发死亡动画并标记为死亡
        if (playerController != null)
        {
            playerController.TriggerDeathAnimation(hitNormal);
        }

        // 只禁用 CharacterController，让角色不再能移动或与场景碰撞
        // 这样重力也不会再由我们自己的代码施加
        CharacterController cc = GetComponentInParent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
        }
        
        // 禁用这个脚本，防止重复调用
        // this.enabled = false; // 可以暂时不禁用，以便观察
    }
    // ======================================

    // 更新所有UI的方法
    private void UpdateUI()
    {
        UpdateHealthUI(CurrentHealth, maxHealth);
        UpdateStaminaUI(CurrentStamina, maxStamina);
        UpdateFlaskUI();
    }
    
    // 订阅事件来更新UI，而不是在每次消耗时都手动调用
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