using UnityEngine;

// [CreateAssetMenu] 允许你从右键菜单创建这个数据资产
[CreateAssetMenu(fileName = "New Enemy Stats", menuName = "AI/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float chaseSpeed = 5f;

    [Header("AI Behavior")]
    public float sightRange = 15f;
    public float walkRadius = 10f;
    public float patrolWaitTime = 5f;

    [Header("Combat")]
    public float maxHealth = 100f;
    public float attackRange = 2f;
    public float attackCooldown = 3f;
    public float attackDamage = 15f;
    public float attackDamageRadius = 1.5f;

    [Header("Special Behaviors")]
    public bool hasScreamAndCharge = false;
    public float chargeDuration = 999f; // 你可以保持一个很大的值
    // ======[ 新增一个开关 ]======
    [Tooltip("如果为 true，该敌人的常规追击会被冲锋取代")]
    public bool replaceChaseWithCharge = false;
}