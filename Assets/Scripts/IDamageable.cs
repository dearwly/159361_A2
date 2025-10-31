using UnityEngine;

public interface IDamageable
{
    // 任何可受伤的物体，都必须实现这个方法
    // damage: 受到的伤害值
    // hitPoint: 攻击命中的位置（用于特效）
    // hitNormal: 攻击命中的法线方向（用于特效和击退）
    void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal);
}