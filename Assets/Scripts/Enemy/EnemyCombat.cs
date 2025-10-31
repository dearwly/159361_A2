using UnityEngine;
using UnityEngine.AI;

// 让这个脚本也实现 IDamageable
public class EnemyCombat : MonoBehaviour, IDamageable
{
    private EnemyStats stats;
    private Animator animator;
    private EnemyController controller; // 引用控制器来改变状态

    private float currentHealth;
    private float attackTimer;

    public void Initialize(EnemyStats enemyStats, Animator anim)
    {
        this.stats = enemyStats;
        this.animator = anim;
        this.controller = GetComponent<EnemyController>();
        currentHealth = stats.maxHealth;
    }
    
    void Update()
    {
        attackTimer += Time.deltaTime;
    }

    public void Attack(Transform target)
    {
        if (attackTimer >= stats.attackCooldown)
        {
            attackTimer = 0f;
            animator.SetTrigger("Attack");
            // 确保敌人面向玩家
            transform.LookAt(target);
        }
    }

    // 由动画事件调用的伤害方法 (逻辑和你原来的一样)
    public void AnimationEvent_DealDamage()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward, stats.attackDamageRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                hitCollider.GetComponentInParent<IDamageable>()?.TakeDamage(stats.attackDamage, transform.position, transform.forward);
                break;
            }
        }
    }

    // 实现受伤接口
    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (currentHealth <= 0 && !this.enabled) return; 
        currentHealth -= damage;
        animator.SetTrigger("TakeHit");

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    private void Die()
    {
        Debug.Log(gameObject.name + " has died.");


        // 触发死亡动画
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        // 禁用AI大脑和导航，让它停止思考和寻路
        if (controller != null)
        {
            controller.enabled = false;
        }
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
        }

        // 禁用这个战斗脚本本身
        this.enabled = false;

        // 【核心修改】启动一个协程来处理死亡后的物理变化
        StartCoroutine(HandleDeathPhysics());
    }

    // ======[ 新增：处理死亡物理的协程 ]======
    private System.Collections.IEnumerator HandleDeathPhysics()
    {
        // 1. 等待一段时间，确保死亡动画有足够的时间播放
        //    你可以根据你的死亡动画长度来调整这个时间
        yield return new WaitForSeconds(2.5f); // 比如等待2.5秒

        // 2. 动画播放得差不多了，现在处理物理状态

        // 获取碰撞体
        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null)
        {
            // 禁用主碰撞体。注意：如果你希望尸体能和其他物体碰撞，可以不禁用它
            // mainCollider.enabled = false; 
        }
        
        // 3. 添加刚体，但这次要让它变成运动学(Kinematic)的，直到动画完全结束
        //    这样可以防止它在动画播放时乱动或掉落
        if (GetComponent<Rigidbody>() == null) // 检查是否已经有了Rigidbody
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // 设为运动学，不受物理影响，但可以被动画移动
            rb.useGravity = false; // 关闭重力
        }

        // 4. (可选) 再等待动画完全结束
        yield return new WaitForSeconds(1.0f); // 比如再等1秒让动画播完

        // 5. (可选) 动画彻底结束后，再开启物理效果，让它变成一个真正的“布娃娃”
        Rigidbody finalRb = GetComponent<Rigidbody>();
        if (finalRb != null)
        {
            finalRb.isKinematic = false;
            finalRb.useGravity = true;
        }
    }
}