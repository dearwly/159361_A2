using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems; 

[RequireComponent(typeof(CharacterController), typeof(AudioSource))] 
public class PlayerController : MonoBehaviour
{
    
    private CharacterController controller;
    private Animator animator;
    private Transform mainCameraTransform;
    private PlayerStats playerStats;
    private AudioSource audioSource; 

    
    public bool isDead = false;
    private bool sprintInputHeld = false;

    
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float runSpeed = 7.0f;

    [Header("Rotation Settings")]
    [SerializeField] private float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;

    [Header("Physics")]
    [SerializeField] private float gravityValue = -9.81f;
    private float verticalVelocity;
    [SerializeField] private float terminalVelocity = -10.0f;
    [SerializeField] private float groundCheckDistance = 0.3f;

    [Header("Combat")]
    [SerializeField] private float blockSpeedModifier = 0.5f;
    [SerializeField] private float attackCost = 20f;
    [SerializeField] private float runCost = 15f;
    public Collider weaponCollider;

    [Header("Weapon Skill")]
    public GameObject shockwaveVFXPrefab;
    public GameObject swordGlowVFX;
    public Transform shockwaveSpawnPoint;

    
    [Header("Sound Effects")]
    [Tooltip("将单个挥剑音效文件拖到这里")]
    public AudioClip swingSound; 

    
    public bool IsGrounded { get; private set; }
    public bool isAttacking => animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack");
    public bool IsInAirborneState => animator.GetCurrentAnimatorStateInfo(0).IsTag("Airborne");
    public bool IsRolling => animator.GetCurrentAnimatorStateInfo(0).IsTag("Rolling");
    public bool IsBlocking => animator.GetBool("isBlocking");
    public bool IsHealing => animator.GetCurrentAnimatorStateInfo(0).IsTag("Healing");
    public bool IsPoweringUp => animator.GetCurrentAnimatorStateInfo(0).IsTag("PoweringUp");
    public bool IsCastingSkill => animator.GetCurrentAnimatorStateInfo(0).IsTag("SkillCasting");


    private Vector2 moveInput;

    private void Awake()
    {
        
        playerStats = GetComponent<PlayerStats>();
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        mainCameraTransform = Camera.main.transform;
        
        
        audioSource = GetComponent<AudioSource>();
        if(audioSource == null)
        {
            
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; 
        }

        
        GetComponentInChildren<WeaponDamage>().SetOwner(this.gameObject);
    }

    
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        
        
        if (context.performed && !IsInAirborneState && !IsRolling && !IsBlocking && playerStats.HasEnoughStamina(attackCost) && !IsCastingSkill)
        {
            
            animator.SetTrigger("Attack");


            
        }
    }

    
    public void OnSprint(InputAction.CallbackContext context)
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (context.performed) { sprintInputHeld = true; }
        else if (context.canceled) { sprintInputHeld = false; }
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    public void OnBlock(InputAction.CallbackContext context)
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (context.performed)
        {
            if (IsGrounded && !isAttacking && !IsRolling && !IsCastingSkill) { animator.SetBool("isBlocking", true); }
        }
        else if (context.canceled)
        {
            animator.SetBool("isBlocking", false);
        }
    }
    public void OnPowerUp(InputAction.CallbackContext context)
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (context.performed && IsGrounded && !isAttacking && !IsBlocking && !IsRolling && !IsHealing && !IsPoweringUp && !IsCastingSkill)
        {
            animator.SetTrigger("PowerUp");
        }
    }
    public void OnWeaponSkill(InputAction.CallbackContext context)
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (context.performed && IsGrounded && !isAttacking && !IsBlocking && !IsRolling && !IsHealing && !IsPoweringUp && !IsCastingSkill)
        {
            animator.SetTrigger("WeaponSkill");
        }
    }
    public void OnRoll(InputAction.CallbackContext context)
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (context.performed && IsGrounded && !isAttacking && !IsRolling && !IsBlocking && !IsCastingSkill)
        {
            animator.SetTrigger("Roll");
        }
    }
    public void OnUseItem(InputAction.CallbackContext context)
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (context.performed && IsGrounded && !isAttacking && !IsBlocking && !IsRolling && !IsHealing && !IsPoweringUp && !IsCastingSkill)
        {
            if (playerStats.currentFlasks > 0 && playerStats.CurrentHealth < playerStats.maxHealth) { animator.SetTrigger("UseFlask"); }
            else { Debug.Log("No flasks or health is full."); }
        }
    }
    
    
    void Update()
    {
        if (isDead) { animator.applyRootMotion = true; return; }
        
        HandleCursor();
        GroundCheck();
        HandleGravity();

        if (IsCastingSkill) { animator.applyRootMotion = false; }
        else { animator.applyRootMotion = isAttacking || IsRolling; }

        if (IsHealing || IsPoweringUp) { HandleLimitedMovement(); }
        else if (!isAttacking && !IsInAirborneState && !IsRolling && !IsBlocking) { HandleMovementAndRotation(); }

        UpdateAnimator();
    }

    
    private void HandleCursor()
    {
        if (Time.timeScale == 0f) { return; }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    private void OnAnimatorMove()
    {
        if (!animator.applyRootMotion) return;
        controller.Move(animator.deltaPosition);
    }
    private void HandleGravity()
    {
        if (IsGrounded && verticalVelocity < 0.0f) { verticalVelocity = -2f; }
        else if (verticalVelocity > terminalVelocity) { verticalVelocity += gravityValue * Time.deltaTime; }
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

            bool isActuallySprinting = sprintInputHeld && !IsBlocking && playerStats.HasEnoughStamina(runCost * Time.deltaTime);
            float targetSpeed = isActuallySprinting ? runSpeed : walkSpeed;
            if (isActuallySprinting) { playerStats.ConsumeStamina(runCost * Time.deltaTime); }
            if (IsBlocking) { targetSpeed *= blockSpeedModifier; }
            
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
        if (playerStats != null) { playerStats.ConsumeStamina(attackCost); }
    }
    public void AnimationEvent_EnableWeaponCollider()
    {
        if (weaponCollider != null) { Debug.Log("开启武器碰撞"); weaponCollider.enabled = true; }
    }
    


    public void AnimationEvent_PlaySwingSound()
    {
        
        if (audioSource != null && swingSound != null)
        {
            
            audioSource.PlayOneShot(swingSound);
        }
    }

    public void AnimationEvent_DisableWeaponCollider()
    {
        if (weaponCollider != null) { Debug.Log("关闭武器碰撞"); weaponCollider.enabled = false; }
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
        animator.SetFloat("MoveMagnitude", moveInput.magnitude);
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
        if (IsBlocking) { animator.SetTrigger("BlockImpact"); return; }
        float dotProduct = Vector3.Dot(transform.forward, -hitNormal);
        animator.SetTrigger(dotProduct > 0 ? "HitFront" : "HitBack");
    }
    public void TriggerDeathAnimation(Vector3 hitNormal)
    {
        if (isDead) return;
        isDead = true;
        float dotProduct = Vector3.Dot(transform.forward, -hitNormal);
        this.enabled = false;
        GetComponent<CharacterController>().enabled = false;
        animator.SetTrigger(dotProduct > 0 ? "DeathFront" : "DeathBack");
    }
    private void OnDestroy()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}