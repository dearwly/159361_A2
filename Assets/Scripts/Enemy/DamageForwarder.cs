using UnityEngine;

// 这个脚本将作为伤害的“中转站”
public class DamageForwarder : MonoBehaviour, IDamageable
{
    // 在 Inspector 里，把真正处理伤害的对象拖到这里
    [SerializeField] private GameObject targetObject;

    private IDamageable _damageableTarget;

    void Awake()
    {
        if (targetObject != null)
        {
            // 从目标对象上获取 IDamageable 接口
            _damageableTarget = targetObject.GetComponent<IDamageable>();
        }
    }

    // 实现 IDamageable 接口
    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        // 检查目标是否存在
        if (_damageableTarget != null)
        {
            // 如果存在，就把伤害信号“转发”给它
            Debug.Log(gameObject.name + " 接收到伤害，并转发给 " + targetObject.name);
            _damageableTarget.TakeDamage(damage, hitPoint, hitNormal);
        }
        else
        {
            Debug.LogWarning("伤害接收器 " + gameObject.name + " 没有设置伤害目标！", this);
        }
    }
}