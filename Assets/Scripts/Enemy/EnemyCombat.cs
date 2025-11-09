using UnityEngine;
using UnityEngine.AI;


public class EnemyCombat : MonoBehaviour, IDamageable
{
    private EnemyStats stats;
    private Animator animator;
    private EnemyController controller; 

    private float currentHealth;
    private float attackTimer;
    private bool isDead = false;

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
            
            transform.LookAt(target);
        }
    }

    
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
        
        if (isDead) return;
        isDead = true; 

        Debug.Log(gameObject.name + " has died.");

        
        
        SetLayerRecursively(this.gameObject, LayerMask.NameToLayer("DeadBody"));
        

        
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        
        if (controller != null)
        {
            controller.enabled = false;
        }

        
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.enabled = false;
        }

        
        this.enabled = false;

        
        StartCoroutine(HandleDeathPhysics());
    }

    
    private System.Collections.IEnumerator HandleDeathPhysics()
    {
        
        
        
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsTag("Death")); 
        
        yield return new WaitWhile(() => animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f);

        

        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        
        
        rb.isKinematic = false; 
        rb.useGravity = true;  
        rb.constraints = RigidbodyConstraints.FreezeAll; 
    }


    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}