using UnityEngine;
using UnityEngine.AI; // 必须引用AI命名空间

public enum AIState { Idle, Patrolling, Chasing, Screaming, Charging, Attacking }

// 确保游戏对象上有关联的组件
[RequireComponent(typeof(NavMeshAgent), typeof(Animator), typeof(EnemyCombat))]
public class EnemyController : MonoBehaviour
{
    // === 核心组件引用 ===
    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;
    private EnemyCombat combat; // 对战斗模块的引用

    // === AI 状态机 ===
    // 使用 public 以便在 Inspector 中看到当前状态，方便调试

    public AIState currentState;

    // === 核心数据资产 ===
    [Tooltip("将敌人的数据资产（如 NormalZombie_Stats）拖到这里")]
    public EnemyStats stats; // 直接引用我们的 ScriptableObject 数据资产！

    // === 内部计时器与变量 ===
    private float patrolTimer;
    // 新增计时器
    private float chargeTimer;
    // 新增一个标志位，确保尖叫只在第一次发现时触发
    private bool hasSpottedPlayer = false;
    private Vector3 patrolDestination;

    void Awake()
    {
        // 获取所有必要的组件引用
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        combat = GetComponent<EnemyCombat>();
        
        // 尝试自动寻找玩家，如果找不到，会在控制台给出警告
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
        // 检查 stats 是否被赋值，防止 NullReferenceException
        if (stats == null)
        {
            Debug.LogError("EnemyStats asset is not assigned on " + gameObject.name + ". Disabling AI.", this);
            this.enabled = false;
            return;
        }

        // 初始化战斗模块
        combat.Initialize(stats, animator);

        // 初始化状态和导航代理
        currentState = AIState.Idle;
        patrolTimer = stats.patrolWaitTime;
        agent.speed = stats.moveSpeed; // 设置初始巡逻速度
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
                else { HandlePatrolBehavior(); } // HandlePatrolBehavior 保持不变
                break;

            case AIState.Screaming:
                // 在尖叫状态下，AI是完全停止的，由协程控制后续逻辑
                break;

            case AIState.Charging:
            case AIState.Chasing: // 将冲锋和追击逻辑合并，因为它们都是在移动
                // 在追击/冲锋状态下，持续更新目标点
                agent.SetDestination(player.position);

                if (distanceToPlayer > stats.sightRange) { TransitionToState(AIState.Idle); }
                else if (distanceToPlayer <= stats.attackRange) { TransitionToState(AIState.Attacking); }
                break;

            case AIState.Attacking:
                // 在攻击状态下，AI 停止移动，并始终朝向玩家
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
        
        // 更新动画参数
        // 关键：即使 agent.updatePosition 为 false，agent.velocity 依然会提供期望的速度值
        animator.SetFloat("Speed", agent.velocity.magnitude);
    }
    

    // 封装的状态转换方法，方便管理
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
                agent.isStopped = false; // 追击时必须移动！
                agent.speed = stats.chaseSpeed;
                break;
            case AIState.Patrolling:
                agent.isStopped = false;
                agent.speed = stats.moveSpeed;
                break;
            case AIState.Attacking:
                // 进入攻击状态时，确保代理是停止的，以便播放动画
                agent.isStopped = true;
                agent.speed = 0;
                break;
                // 【新增状态的转换逻辑】
            case AIState.Screaming:
                hasSpottedPlayer = true;
                agent.isStopped = true;
                animator.SetTrigger("Scream");
                // 【核心修改】我们在这里直接启动一个协程来等待动画结束
                StartCoroutine(ScreamFinishedRoutine());
                break;
                
            case AIState.Charging:
                agent.isStopped = false;
                agent.speed = stats.chaseSpeed; // 使用冲锋速度
                chargeTimer = stats.chargeDuration; // 重置冲锋计时器
                break;
        }
    }


    private System.Collections.IEnumerator ScreamFinishedRoutine()
    {
        // --- 第一阶段：等待，直到我们确认已经进入了 Scream 动画状态 ---
        // 这是一个安全措施，防止在动画过渡期间就错误地跳过了等待
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("scream"))
        {
            // 只要当前动画还不是 "scream"，就一直等待下一帧
            yield return null;
        }

        // --- 第二阶段：等待 Scream 动画播放完毕 ---
        // 现在我们确定已经在 Scream 状态了，再开始监测它的播放进度
        // 我们等待动画的归一化时间（normalizedTime）大于等于 0.9 (即90%)
        while (animator.GetCurrentAnimatorStateInfo(0).IsName("scream") &&
            animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.9f)
        {
            yield return null; // 等待下一帧
        }
        
        // --- 第三阶段：动画播放完毕，切换到下一个AI状态 ---
        Debug.Log("Scream finished, now charging!"); // 添加一条日志来确认

        if (stats.replaceChaseWithCharge)
        {
            TransitionToState(AIState.Charging);
        }
        else
        {
            TransitionToState(AIState.Chasing);
        }
    }

    // 处理待机和巡逻的逻辑
    private void HandlePatrolBehavior()
    {
        // 如果当前是待机状态
        if (currentState == AIState.Idle)
        {
            patrolTimer += Time.deltaTime;
            // 等待时间足够，切换到巡逻状态并寻找新目标点
            if (patrolTimer >= stats.patrolWaitTime)
            {
                TransitionToState(AIState.Patrolling);
                FindNewPatrolPoint();
            }
        }
        // 如果当前是巡逻状态，检查是否已到达目的地
        else if (currentState == AIState.Patrolling)
        {
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                // 到达目的地，切换回待机状态
                TransitionToState(AIState.Idle);
            }
        }
    }

    // 寻找一个新的随机巡逻点
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
            // 如果找不到有效的点，直接回到待机
            TransitionToState(AIState.Idle);
        }
    }
}