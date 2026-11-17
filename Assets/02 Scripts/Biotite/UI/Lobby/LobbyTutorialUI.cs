using UnityEngine;



//튜토리얼에 관한 ui 입니다.
public class LobbyTutorialUI : BaseUI
{
    [Header("Button")] 
    [SerializeField] private AnimButton closeButton;
    
    [Header("Animator")]
    [SerializeField] private Animator animator;
 
    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);
        BindEvents();
    }

    
    public void OpenAnimationFinishEvent()
    {
        animator.enabled = false;
    }

    private void OnDestroy()
    {
        closeButton.OnClicked -= CloseUI;
    }

    private void BindEvents()
    {
        closeButton.OnClicked += CloseUI;
    }

}
