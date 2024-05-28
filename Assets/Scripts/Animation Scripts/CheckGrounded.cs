using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckGrounded : StateMachineBehaviour
{
    [SerializeField] private bool grounded;
    [SerializeField] private bool unlockLayer;
    [SerializeField] private Animations animation;
    [SerializeField] private bool lockLayer;
    [SerializeField] private float corssfade = 0.2f;

    private AnimatorBrain animatorBrain;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animatorBrain = animator.GetComponent<AnimatorBrain>();
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (grounded == animatorBrain.Grounded)
        {
            animatorBrain.SetLocked(!unlockLayer, layerIndex);
            animatorBrain.Play(animation, layerIndex, lockLayer, false, corssfade);
        }
    }
}
