using UnityEngine;

public class UpdateAttackCombo : StateMachineBehaviour
{
    [Header("连击设置")]
    public int comboStep; // 在Inspector里为每个攻击状态设置对应的连击数

    [Header("状态设置")]
    [Tooltip("勾选此项，进入该状态时会重置连击数")]
    public bool shouldResetCombo = false; // 默认不重置

    // 当进入这个动画状态时调用
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 检查是否需要重置连击
        if (shouldResetCombo)
        {
            // 如果勾选了，就将连击数清零
            animator.SetInteger("AttackCombo", 0);
        }
        else
        {
            // 如果没勾选（说明这是个攻击状态），就更新连击数
            animator.SetInteger("AttackCombo", comboStep);
        }
        
        // 无论如何，都消耗掉“攻击”信号，防止在同一帧内重复触发
        animator.ResetTrigger("Attack");
    }
}