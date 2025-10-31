using UnityEngine;
using System.Collections.Generic;

public class ShockwaveDamage : MonoBehaviour
{
    public float damage = 40f;
    public float radius = 5f; // 圆形伤害的半径
    
    private List<Collider> alreadyHit = new List<Collider>();

    void OnEnable()
    {
        alreadyHit.Clear();
        DealDamage();
    }

    void DealDamage()
    {
        // 在特效的中心点创建一个球体，检测范围内的所有碰撞体
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);

        foreach (var hitCollider in hitColliders)
        {
            // 【核心修改】移除了角度判断
            // 只要是敌人，并且没被命中过，就造成伤害
            if (hitCollider.CompareTag("Enemy") && !alreadyHit.Contains(hitCollider))
            {
                IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>(); // 使用 GetComponentInParent 更稳健
                if (damageable != null)
                {
                    // 伤害方向可以简单地设置为从中心指向敌人
                    Vector3 directionToTarget = (hitCollider.transform.position - transform.position).normalized;
                    damageable.TakeDamage(damage, hitCollider.transform.position, directionToTarget);
                    alreadyHit.Add(hitCollider);
                }
            }
        }
    }
    
    // 在编辑器中显示伤害范围，现在是一个圆圈
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius); // 直接画一个球体
    }
}