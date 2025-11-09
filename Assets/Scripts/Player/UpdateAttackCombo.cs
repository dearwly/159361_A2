using UnityEngine;

public class UpdateAttackCombo : StateMachineBehaviour
{
    [Header("连击设置")]
    public int comboStep; 

    [Header("状态设置")]
    [Tooltip("勾选此项，进入该状态时会重置连击数")]
    public bool shouldResetCombo = false; 

    
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
        if (shouldResetCombo)
        {
            
            animator.SetInteger("AttackCombo", 0);
        }
        else
        {
            
            animator.SetInteger("AttackCombo", comboStep);
        }
        
        
        animator.ResetTrigger("Attack");
    }
}