using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // ... (你之前的变量声明都保持不变)
    private CharacterController controller;
    private Animator animator;
    private Transform mainCameraTransform;
    private Vector2 moveInput;
    private PlayerStats playerStats;
    public bool isDead = false; // 新增：死亡状态标志

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

    [Header("Ground Check Settings")] // 为新变量创建一个分类
    [SerializeField] private float groundCheckDistance = 0.3f; // 检测球体的半径

    [Header("Blocking Settings")]
    [SerializeField] private float blockSpeedModifier = 0.5f; // 举盾时的移动速度倍率
    [Header("Stamina Settings")]
    [SerializeField] private float attackCost = 20f;
    [SerializeField] private float runCost = 15f;
    [Header("Combat")]
    public Collider weaponCollider; // 用于开关武器的碰撞体
    [Header("Weapon Skill")]
    public GameObject shockwaveVFXPrefab; // 震地波特效预制件
    public GameObject swordGlowVFX; // 剑发光特效
    public Transform shockwaveSpawnPoint; // 震地波生成的位置（比如角色脚下）



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
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        GetComponentInChildren<WeaponDamage>().SetOwner(this.gameObject);
    }

    // ... (OnAttack, OnSprint, OnMove 保持不变)
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed && !IsInAirborneState && !IsRolling && !IsBlocking && playerStats.HasEnoughStamina(attackCost) && !IsCastingSkill)
        {
            animator.SetTrigger("Attack");
        }
    }
    public void OnSprint(InputAction.CallbackContext context)
    {
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
        // 按下时
        if (context.performed)
        {
            // 只有在地面上，且不处于其他动作时才能举盾
            if (IsGrounded && !isAttacking && !IsRolling  && !IsCastingSkill)
            {
                animator.SetBool("isBlocking", true);
            }
        }
        // 松开时
        else if (context.canceled)
        {
            animator.SetBool("isBlocking", false);
        }
    }

    public void OnPowerUp(InputAction.CallbackContext context)
    {
        // 可以在不处于其他关键动作时使用
        if (context.performed && IsGrounded && !isAttacking && !IsBlocking && !IsRolling && !IsHealing && !IsPoweringUp && !IsCastingSkill)
        {
            animator.SetTrigger("PowerUp");
        }
    }

    public void OnWeaponSkill(InputAction.CallbackContext context)
    {
        // 可以在地面上，且不处于其他关键动作时使用
        if (context.performed && IsGrounded && !isAttacking && !IsBlocking && !IsRolling && !IsHealing && !IsPoweringUp && !IsCastingSkill)
        {
            animator.SetTrigger("WeaponSkill");
        }
    }

    void Update()
    {
        if (isDead)
        {
            animator.applyRootMotion = true; // 死亡时让根运动接管
            return;
        }

        
        //落地检测
        GroundCheck();
        // 我们只在这里处理重力，以及非攻击状态下的移动
        HandleGravity();

        animator.applyRootMotion = isAttacking; // 核心：根据是否攻击来开关根运动

        if (IsCastingSkill)
        {
            animator.applyRootMotion = false; 
        }
        else // 否则，执行常规的逻辑
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

    // ======[ 恢复并优化 OnAnimatorMove() ]======
    private void OnAnimatorMove()
    {
        // 如果根运动没有被激活（即不在攻击状态），则不执行任何操作
        if (!animator.applyRootMotion) return;

        // 当根运动被激活时（攻击中），使用动画提供的位移
        // 这会同时包含水平位移（第4段攻击）和垂直位移（第5段跳劈）
        controller.Move(animator.deltaPosition);
    }

    private void HandleGravity()
    {
        // 如果角色在地面上
        if (IsGrounded && verticalVelocity < 0.0f)
        {
            verticalVelocity = -2f; // 重置垂直速度
        }
        else // 如果角色在空中
        {
            // 只有当当前下落速度还没有达到我们的“终端速度”时，才继续施加重力（加速）
            if (verticalVelocity > terminalVelocity)
            {
                verticalVelocity += gravityValue * Time.deltaTime;
            }
        }

        // 将最终计算出的垂直位移应用到角色
        controller.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
    }

    private void HandleMovementAndRotation()
    {
        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        if (inputDirection.magnitude >= 0.1f)
        {
            // === 旋转逻辑 (保持不变) ===
            float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + mainCameraTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // === 速度与耐力计算逻辑 (全新) ===
            float targetSpeed;

            // 1. 判断当前帧是否“正在冲刺”
            // 条件：玩家按下了冲刺键 && 不是在防御 && 有足够的耐力
            bool isActuallySprinting = sprintInputHeld && !IsBlocking && playerStats.HasEnoughStamina(runCost * Time.deltaTime);

            // 2. 根据是否“正在冲刺”来设置速度和消耗耐力
            if (isActuallySprinting)
            {
                targetSpeed = runSpeed;
                playerStats.ConsumeStamina(runCost * Time.deltaTime);
            }
            else
            {
                targetSpeed = walkSpeed;
            }

            // 3. 如果正在防御，则在计算出的速度基础上再乘以防御减速倍率
            if (IsBlocking)
            {
                targetSpeed *= blockSpeedModifier;
            }

            // 4. 将最终计算出的速度应用到动画器
            // 我们需要告诉动画器当前是否在冲刺，以便播放正确的动画
            animator.SetBool("isSprinting", isActuallySprinting);

            // === 应用最终的移动 ===
            Vector3 moveDirection = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            controller.Move(moveDirection.normalized * targetSpeed * Time.deltaTime);
        }
        else // 如果没有移动输入
        {
            // 确保在停下时，跑步动画也停止
            animator.SetBool("isSprinting", false);
        }
    }

    private void HandleLimitedMovement()
    {
        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        // 动画参数：确保移动时播放走路动画，停下时播放待机
        animator.SetFloat("MoveMagnitude", inputDirection.magnitude);
        animator.SetBool("isSprinting", false); // 在受限状态下，永远不能冲刺

        if (inputDirection.magnitude >= 0.1f)
        {
            // 处理旋转
            float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + mainCameraTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // 只应用走路速度来移动
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
            Debug.Log("开启武器碰撞"); // 添加日志来确认方法被调用
            weaponCollider.enabled = true;
        }
    }

    // 这个方法将被动画事件在攻击结束时调用
    public void AnimationEvent_DisableWeaponCollider()
    {
        if (weaponCollider != null)
        {
            Debug.Log("关闭武器碰撞"); // 添加日志来确认方法被调用
            weaponCollider.enabled = false;
        }
    }

    public void AnimationEvent_ExecuteWeaponSkill()
    {
        StartCoroutine(WeaponSkillSequence());
    }

    private System.Collections.IEnumerator WeaponSkillSequence()
    {
        // 1. 剑变红
        if (swordGlowVFX != null) swordGlowVFX.SetActive(true);

        // 等待一小段时间，让特效显示出来
        yield return new WaitForSeconds(0.5f); 

        // 2. 连续释放三次震地波
        for (int i = 0; i < 3; i++)
        {
            // 生成震地波特效
            if (shockwaveVFXPrefab != null && shockwaveSpawnPoint != null)
            {
                // 确保震地波朝向角色前方
                Instantiate(shockwaveVFXPrefab, shockwaveSpawnPoint.position, transform.rotation);
            }
            
            // 每次释放之间间隔一小段时间
            yield return new WaitForSeconds(0.4f); 
        }

        // 3. 技能结束，关闭剑的特效
        if (swordGlowVFX != null) swordGlowVFX.SetActive(false);
    }

    private void UpdateAnimator()
    {
        float magnitude = moveInput.magnitude;
        animator.SetFloat("MoveMagnitude", magnitude);
        // animator.SetBool("isSprinting", isSprinting && magnitude > 0);

        animator.SetBool("isGrounded", IsGrounded);
    }

    private void GroundCheck()
    {
        // 1. 在角色脚底创建一个检测球体，获取所有碰到的碰撞体
        Collider[] colliders = Physics.OverlapSphere(transform.position, groundCheckDistance);

        // 2. 遍历所有碰到的碰撞体
        IsGrounded = false; // 先假设自己不在地面上
        if (colliders.Length > 0)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                // 3.【核心逻辑】检查碰到的物体是不是玩家自己
                // 我们使用 CompareTag("Player") 来识别玩家。
                // 如果碰到的物体不是玩家自己，那么就说明我们碰到了“地面”或“障碍物”。
                if (colliders[i].gameObject.CompareTag("Player") == false)
                {
                    IsGrounded = true; // 找到了地面，立刻将状态设为 true
                    break; // 并且跳出循环，因为只要找到一个地面就够了，没必要再检查其他的
                }
            }
        }
    }

    public void OnRoll(InputAction.CallbackContext context)
    {
        // 只有在按下瞬间，并且在地面上，并且没有在进行其他动作时，才允许翻滚
        if (context.performed && IsGrounded && !isAttacking && !IsRolling && !IsBlocking  && !IsCastingSkill)
        {
            animator.SetTrigger("Roll");
        }
    }
    // ======[ 新增：触发受击/死亡动画的方法 ]======
    public void TriggerHitAnimation(Vector3 hitNormal)
    {
        // 如果正在格挡，则播放格挡受击动画
        if (IsBlocking)
        {
            animator.SetTrigger("BlockImpact");
            return; // 后续逻辑不再执行
        }

        // 计算攻击方向与角色朝向的点积
        // 点积 > 0: 攻击来自前方
        // 点积 < 0: 攻击来自后方
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

    // 死亡逻辑也可以放在这里
    public void TriggerDeathAnimation(Vector3 hitNormal)
    {
        if (isDead) return;
        isDead = true; // 标记为死亡
        float dotProduct = Vector3.Dot(transform.forward, -hitNormal);

        // 禁用角色控制器和AI
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

    // ======[ 新增：使用道具的输入处理方法 ]======
    public void OnUseItem(InputAction.CallbackContext context)
    {
        if (context.performed && IsGrounded && !isAttacking && !IsBlocking && !IsRolling && !IsHealing && !IsPoweringUp && !IsCastingSkill)
        {
            // 只检查条件，不再调用任何消耗方法
            if (playerStats.currentFlasks > 0 && playerStats.CurrentHealth < playerStats.maxHealth)
            {
                // 条件满足，直接触发动画
                animator.SetTrigger("UseFlask");
            }
            else
            {
                Debug.Log("No flasks or health is full. Animation not triggered.");
            }
        }
    }
}