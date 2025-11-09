using UnityEngine;


public class WeaponDamage : MonoBehaviour
{
    public float baseDamageAmount = 25f; 
    private Collider weaponCollider;
    private GameObject owner; 
    private PlayerStats ownerStats; 

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
        
        if (owner != null && (other.gameObject == owner || other.transform.IsChildOf(owner.transform)))
        {
            return; 
        }
        
        
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