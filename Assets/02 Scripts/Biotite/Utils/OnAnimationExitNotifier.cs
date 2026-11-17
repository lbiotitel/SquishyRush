using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//애니메이션 클립이 끝날때 이벤트를 호출합니다.
public class OnAnimationExitNotifier : StateMachineBehaviour
{
    public event Action OnAnimExit;

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnAnimExit?.Invoke();
    }
}
