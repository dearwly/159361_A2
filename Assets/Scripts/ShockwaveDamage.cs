using UnityEngine;
using System.Collections.Generic;

public class ShockwaveDamage : MonoBehaviour
{
    public float damage = 40f;
    public float radius = 5f; 
    
    private List<Collider> alreadyHit = new List<Collider>();

    void OnEnable()
    {
        alreadyHit.Clear();
        DealDamage();
    }

    void DealDamage()
    {
        
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);

        foreach (var hitCollider in hitColliders)
        {
            
            
            if (hitCollider.CompareTag("Enemy") && !alreadyHit.Contains(hitCollider))
            {
                IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>(); 
                if (damageable != null)
                {
                    
                    Vector3 directionToTarget = (hitCollider.transform.position - transform.position).normalized;
                    damageable.TakeDamage(damage, hitCollider.transform.position, directionToTarget);
                    alreadyHit.Add(hitCollider);
                }
            }
        }
    }
    
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius); 
    }
}