using UnityEngine;
using UnityEngine.AI; // 必须引用AI命名空间

public enum AIState { Idle, Patrolling, Chasing, Screaming, Charging, Attacking }

// ======[ 【核心修改】将 RequireComponent 拆分成多行 ]======
// 确保游戏对象上有关联的组件
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyCombat))]
[RequireComponent(typeof(AudioSource))]
// =========================================================
public class EnemyController : MonoBehaviour
{
    // === 核心组件引用 ===
    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;
    private EnemyCombat combat;
    private AudioSource audioSource;

    // === AI 状态机 ===
    public AIState currentState;

    // === 核心数据资产 ===
    [Tooltip("将敌人的数据资产（如 NormalZombie_Stats）拖到这里")]
    public EnemyStats stats;

    // === 音效设置 ===
    [Header("Sound Effects")]
    [Tooltip("将僵尸的尖叫音效文件拖到这里")]
    public AudioClip screamSound;

    // === 内部计时器与变量 ===
    private float patrolTimer;
    private float chargeTimer;
    private bool hasSpottedPlayer = false;
    private Vector3 patrolDestination;

    void Awake()
    {
        // 获取所有必要的组件引用
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        combat = GetComponent<EnemyCombat>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f;
        }
        
        // 尝试自动寻找玩家
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogError("AI Controller cannot find GameObject with Tag 'Player'. Make sure the player exists and is tagged correctly.", this);
        }
    }

    void Start()
    {
        if (stats == null)
        {
            Debug.LogError("EnemyStats asset is not assigned on " + gameObject.name + ". Disabling AI.", this);
            this.enabled = false;
            return;
        }

        combat.Initialize(stats, animator);
        currentState = AIState.Idle;
        patrolTimer = stats.patrolWaitTime;
        agent.speed = stats.moveSpeed;
    }

   void Update()
    {
        if (stats == null || player == null || !agent.isOnNavMesh) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case AIState.Idle:
            case AIState.Patrolling:
                if (distanceToPlayer <= stats.sightRange)
                {
                    if (stats.hasScreamAndCharge && !hasSpottedPlayer) { TransitionToState(AIState.Screaming); }
                    else
                    {
                        if (stats.replaceChaseWithCharge) { TransitionToState(AIState.Charging); }
                        else { TransitionToState(AIState.Chasing); }
                    }
                }
                else { HandlePatrolBehavior(); }
                break;

            case AIState.Screaming:
                // 状态逻辑由协程管理
                break;

            case AIState.Charging:
            case AIState.Chasing:
                agent.SetDestination(player.position);
                if (distanceToPlayer > stats.sightRange) { TransitionToState(AIState.Idle); }
                else if (distanceToPlayer <= stats.attackRange) { TransitionToState(AIState.Attacking); }
                break;

            case AIState.Attacking:
                transform.LookAt(player);
                combat.Attack(player);
                if (distanceToPlayer > stats.attackRange)
                {
                    if (stats.replaceChaseWithCharge) { TransitionToState(AIState.Charging); }
                    else { TransitionToState(AIState.Chasing); }
                }
                break;
        }
        
        if (distanceToPlayer > stats.sightRange) { hasSpottedPlayer = false; }
        
        animator.SetFloat("Speed", agent.velocity.magnitude);
    }
    
    public void TransitionToState(AIState newState)
    {
        currentState = newState;
        switch (newState)
        {
            case AIState.Idle:
                agent.isStopped = true;
                patrolTimer = 0;
                agent.speed = stats.moveSpeed;
                break;
            case AIState.Chasing:
                agent.isStopped = false;
                agent.speed = stats.chaseSpeed;
                break;
            case AIState.Patrolling:
                agent.isStopped = false;
                agent.speed = stats.moveSpeed;
                break;
            case AIState.Attacking:
                agent.isStopped = true;
                agent.speed = 0;
                break;
            
            case AIState.Screaming:
                hasSpottedPlayer = true;
                agent.isStopped = true;
                animator.SetTrigger("Scream");

                if (audioSource != null && screamSound != null)
                {
                    if (!audioSource.isPlaying)
                    {
                        audioSource.PlayOneShot(screamSound);
                    }
                }

                StartCoroutine(ScreamFinishedRoutine());
                break;
                
            case AIState.Charging:
                agent.isStopped = false;
                agent.speed = stats.chaseSpeed;
                chargeTimer = stats.chargeDuration;
                break;
        }
    }

    private System.Collections.IEnumerator ScreamFinishedRoutine()
    {
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("scream"))
        {
            yield return null;
        }

        while (animator.GetCurrentAnimatorStateInfo(0).IsName("scream") &&
            animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.9f)
        {
            yield return null;
        }
        
        Debug.Log("Scream finished, now charging!");

        if (stats.replaceChaseWithCharge) { TransitionToState(AIState.Charging); }
        else { TransitionToState(AIState.Chasing); }
    }

    private void HandlePatrolBehavior()
    {
        if (currentState == AIState.Idle)
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= stats.patrolWaitTime)
            {
                TransitionToState(AIState.Patrolling);
                FindNewPatrolPoint();
            }
        }
        else if (currentState == AIState.Patrolling)
        {
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                TransitionToState(AIState.Idle);
            }
        }
    }

    private void FindNewPatrolPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * stats.walkRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, stats.walkRadius, 1))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            TransitionToState(AIState.Idle);
        }
    }
}