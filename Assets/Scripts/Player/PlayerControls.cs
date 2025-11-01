using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems; // <<<<<<< 1. 在顶部添加这一行，非常重要！

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // ... (你所有的变量声明都保持不变)
    private CharacterController controller;
    private Animator animator;
    private Transform mainCameraTransform;
    private Vector2 moveInput;
    private PlayerStats playerStats;
    public bool isDead = false;

    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float runSpeed = 7.0f;
    private bool isSprinting = false;
    [Header("Rotation Settings")]
    [SerializeField] private float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;
    [SerializeField] private float gravityValue = -9.81f;
    private float verticalVelocity;
    [Header("Falling Settings")]
    [SerializeField] private float terminalVelocity = -10.0f;
    [Header("Ground Check Settings")]
    [SerializeField] private float groundCheckDistance = 0.3f;
    [Header("Blocking Settings")]
    [SerializeField] private float blockSpeedModifier = 0.5f;
    [Header("Stamina Settings")]
    [SerializeField] private float attackCost = 20f;
    [SerializeField] private float runCost = 15f;
    [Header("Combat")]
    public Collider weaponCollider;
    [Header("Weapon Skill")]
    public GameObject shockwaveVFXPrefab;
    public GameObject swordGlowVFX;
    public Transform shockwaveSpawnPoint;

    public bool IsGrounded { get; private set; }
    public bool isAttacking => animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack");
    public bool IsInAirborneState => animator.GetCurrentAnimatorStateInfo(0).IsTag("Airborne");
    public bool IsRolling => animator.GetCurrentAnimatorStateInfo(0).IsTag("Rolling");
    public bool IsBlocking => animator.GetBool("isBlocking");
    public bool IsHealing => animator.GetCurrentAnimatorStateInfo(0).IsTag("Healing");
    public bool IsPoweringUp => animator.GetCurrentAnimatorStateInfo(0).IsTag("PoweringUp");
    public bool IsCastingSkill => animator.GetCurrentAnimatorStateInfo(0).IsTag("SkillCasting");
    private bool sprintInputHeld = false;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        mainCameraTransform = Camera.main.transform;
        
        // 我们将鼠标锁定的逻辑移到Update中，以便动态控制
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
        
        GetComponentInChildren<WeaponDamage>().SetOwner(this.gameObject);
    }

    // --- 所有 On... 开头的输入函数都不需要修改 ---
    // (OnAttack, OnSprint, OnMove, OnBlock 等都保持原样)
    public void OnAttack(InputAction.CallbackContext context)
    {
        // <<<<<<< 3. 在所有输入处理函数的最开始，加入UI检查！
        if (EventSystem.current.IsPointerOverGameObject()) return;

        if (context.performed && !IsInAirborneState && !IsRolling && !IsBlocking && playerStats.HasEnoughStamina(attackCost) && !IsCastingSkill)
        {
            animator.SetTrigger("Attack");
        }
    }
    public void OnSprint(InputAction.CallbackContext context)
    {
        if (EventSystem.current.IsPointerOverGameObject()) return; // 同样加入检查

        if (context.performed)
        {
            sprintInputHeld = true;
        }
        else if (context.canceled)
        {
            sprintInputHeld = false;
        }
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    public void OnBlock(InputAction.CallbackContext context)
    {
        if (EventSystem.current.IsPointerOverGameObject()) return; // 同样加入检查

        if (context.performed)
        {
            if (IsGrounded && !isAttacking && !IsRolling && !IsCastingSkill)
            {
                animator.SetBool("isBlocking", true);
            }
        }
        else if (context.canceled)
        {
            animator.SetBool("isBlocking", false);
        }
    }
    public void OnPowerUp(InputAction.CallbackContext context)
    {
        if (EventSystem.current.IsPointerOverGameObject()) return; // 同样加入检查

        if (context.performed && IsGrounded && !isAttacking && !IsBlocking && !IsRolling && !IsHealing && !IsPoweringUp && !IsCastingSkill)
        {
            animator.SetTrigger("PowerUp");
        }
    }
    public void OnWeaponSkill(InputAction.CallbackContext context)
    {
        if (EventSystem.current.IsPointerOverGameObject()) return; // 同样加入检查

        if (context.performed && IsGrounded && !isAttacking && !IsBlocking && !IsRolling && !IsHealing && !IsPoweringUp && !IsCastingSkill)
        {
            animator.SetTrigger("WeaponSkill");
        }
    }
    public void OnRoll(InputAction.CallbackContext context)
    {
        if (EventSystem.current.IsPointerOverGameObject()) return; // 同样加入检查

        if (context.performed && IsGrounded && !isAttacking && !IsRolling && !IsBlocking && !IsCastingSkill)
        {
            animator.SetTrigger("Roll");
        }
    }
    public void OnUseItem(InputAction.CallbackContext context)
    {
        if (EventSystem.current.IsPointerOverGameObject()) return; // 同样加入检查

        if (context.performed && IsGrounded && !isAttacking && !IsBlocking && !IsRolling && !IsHealing && !IsPoweringUp && !IsCastingSkill)
        {
            if (playerStats.currentFlasks > 0 && playerStats.CurrentHealth < playerStats.maxHealth)
            {
                animator.SetTrigger("UseFlask");
            }
            else
            {
                Debug.Log("No flasks or health is full. Animation not triggered.");
            }
        }
    }
    
    void Update()
    {
        if (isDead)
        {
            animator.applyRootMotion = true;
            return;
        }
        
        // <<<<<<< 2. 在Update的开始，加入鼠标状态管理的逻辑
        HandleCursor();
        
        // ... (你所有其他的Update逻辑都保持不变)
        GroundCheck();
        HandleGravity();
        animator.applyRootMotion = isAttacking;
        if (IsCastingSkill)
        {
            animator.applyRootMotion = false; 
        }
        else
        {
            animator.applyRootMotion = isAttacking || IsRolling;
            if (IsHealing || IsPoweringUp)
            {
                HandleLimitedMovement();
            }
            else if (!isAttacking && !IsInAirborneState && !IsRolling && !IsBlocking)
            {
                HandleMovementAndRotation();
            }
        }
        UpdateAnimator();
    }

    // <<<<<<< 2. (续) 这是新增的函数，专门用来处理鼠标状态
    private void HandleCursor()
    {
        // 如果游戏时间是暂停的 (即设置菜单已打开)
        if (Time.timeScale == 0f)
        {
            // 我们什么都不做，将鼠标的控制权完全交给SettingsManager
            return;
        }

        // 否则 (游戏正在运行)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ... (你所有其他的函数，如 OnAnimatorMove, HandleGravity, HandleMovementAndRotation 等，都保持不变)
    
    // ======[ 恢复并优化 OnAnimatorMove() ]======
    private void OnAnimatorMove()
    {
        if (!animator.applyRootMotion) return;
        controller.Move(animator.deltaPosition);
    }
    private void HandleGravity()
    {
        if (IsGrounded && verticalVelocity < 0.0f)
        {
            verticalVelocity = -2f;
        }
        else
        {
            if (verticalVelocity > terminalVelocity)
            {
                verticalVelocity += gravityValue * Time.deltaTime;
            }
        }
        controller.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
    }
    private void HandleMovementAndRotation()
    {
        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        if (inputDirection.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + mainCameraTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            float targetSpeed;
            bool isActuallySprinting = sprintInputHeld && !IsBlocking && playerStats.HasEnoughStamina(runCost * Time.deltaTime);
            if (isActuallySprinting)
            {
                targetSpeed = runSpeed;
                playerStats.ConsumeStamina(runCost * Time.deltaTime);
            }
            else
            {
                targetSpeed = walkSpeed;
            }
            if (IsBlocking)
            {
                targetSpeed *= blockSpeedModifier;
            }
            animator.SetBool("isSprinting", isActuallySprinting);
            Vector3 moveDirection = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            controller.Move(moveDirection.normalized * targetSpeed * Time.deltaTime);
        }
        else
        {
            animator.SetBool("isSprinting", false);
        }
    }
    private void HandleLimitedMovement()
    {
        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        animator.SetFloat("MoveMagnitude", inputDirection.magnitude);
        animator.SetBool("isSprinting", false);
        if (inputDirection.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + mainCameraTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            Vector3 moveDirection = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            controller.Move(moveDirection.normalized * walkSpeed * Time.deltaTime);
        }
    }
    public void Attack_ConsumeStamina()
    {
        if (playerStats != null)
        {
            playerStats.ConsumeStamina(attackCost);
        }
    }
    public void AnimationEvent_EnableWeaponCollider()
    {
        if (weaponCollider != null)
        {
            Debug.Log("开启武器碰撞");
            weaponCollider.enabled = true;
        }
    }
    public void AnimationEvent_DisableWeaponCollider()
    {
        if (weaponCollider != null)
        {
            Debug.Log("关闭武器碰撞");
            weaponCollider.enabled = false;
        }
    }
    public void AnimationEvent_ExecuteWeaponSkill()
    {
        StartCoroutine(WeaponSkillSequence());
    }
    private System.Collections.IEnumerator WeaponSkillSequence()
    {
        if (swordGlowVFX != null) swordGlowVFX.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < 3; i++)
        {
            if (shockwaveVFXPrefab != null && shockwaveSpawnPoint != null)
            {
                Instantiate(shockwaveVFXPrefab, shockwaveSpawnPoint.position, transform.rotation);
            }
            yield return new WaitForSeconds(0.4f);
        }
        if (swordGlowVFX != null) swordGlowVFX.SetActive(false);
    }
    private void UpdateAnimator()
    {
        float magnitude = moveInput.magnitude;
        animator.SetFloat("MoveMagnitude", magnitude);
        animator.SetBool("isGrounded", IsGrounded);
    }
    private void GroundCheck()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, groundCheckDistance);
        IsGrounded = false;
        if (colliders.Length > 0)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].gameObject.CompareTag("Player") == false)
                {
                    IsGrounded = true;
                    break;
                }
            }
        }
    }
    public void TriggerHitAnimation(Vector3 hitNormal)
    {
        if (IsBlocking)
        {
            animator.SetTrigger("BlockImpact");
            return;
        }
        float dotProduct = Vector3.Dot(transform.forward, -hitNormal);
        if (dotProduct > 0)
        {
            animator.SetTrigger("HitFront");
        }
        else
        {
            animator.SetTrigger("HitBack");
        }
    }
    public void TriggerDeathAnimation(Vector3 hitNormal)
    {
        if (isDead) return;
        isDead = true;
        float dotProduct = Vector3.Dot(transform.forward, -hitNormal);
        this.enabled = false;
        GetComponent<CharacterController>().enabled = false;
        if (dotProduct > 0)
        {
            animator.SetTrigger("DeathFront");
        }
        else
        {
            animator.SetTrigger("DeathBack");
        }
    }
    private void OnDestroy()
    {
        // 这是一个“安全网”，确保无论何时玩家对象消失，
        // 鼠标状态都会被重置为适合UI操作的默认状态。
        Time.timeScale = 1f; // 确保时间恢复正常
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}