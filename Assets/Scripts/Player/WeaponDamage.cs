using UnityEngine;

// 这个脚本依然挂载在“剑”上
public class WeaponDamage : MonoBehaviour
{
    public float baseDamageAmount = 25f; // 将名字改为基础伤害
    private Collider weaponCollider;
    private GameObject owner; // 新增：武器的主人
    private PlayerStats ownerStats; // 新增：对主人状态的引用

    void Awake()
    {
        weaponCollider = GetComponent<Collider>();
    }

    public void SetOwner(GameObject ownerObject)
    {
        this.owner = ownerObject;
        if (owner != null)
        {
            ownerStats = owner.GetComponentInParent<PlayerStats>(); 
        }
    }

    
    private void OnTriggerEnter(Collider other)
    {
        // 检查主人是否存在，并且碰到的不是主人自己或主人的子物体
        if (owner != null && (other.gameObject == owner || other.transform.IsChildOf(owner.transform)))
        {
            return; // 如果是自己人，就直接返回，不造成伤害
        }
        
        // 将 GetComponent 改为 GetComponentInParent，这样更稳健
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            Debug.Log("武器碰到了可受伤物体: " + other.name);
            Vector3 contactPoint = other.ClosestPoint(transform.position);
            Vector3 contactNormal = transform.position - other.transform.position;
            float finalDamage = baseDamageAmount;
            if (ownerStats != null && ownerStats.IsPoweredUp)
            {
                finalDamage *= ownerStats.powerUpMultiplier;
                Debug.Log("Powered Up attack! Damage: " + finalDamage);
            }
            damageable.TakeDamage(finalDamage, contactPoint, contactNormal.normalized);
        }
    }
}