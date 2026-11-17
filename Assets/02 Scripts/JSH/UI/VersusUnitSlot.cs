using DG.Tweening;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

public class VersusUnitSlot : MonoBehaviour
{
    private const string ANIM_IDLE = "Idle";

    [SerializeField] private RectTransform body;
    [SerializeField] private SkeletonGraphic skeleton;
    [SerializeField] private Image questionMark;

    public void Set(UnitData data, bool hidden)
    {
        if (data == null || !GlobalManagers.Resource.TryGetResource(data.UnitSpine, out SkeletonDataAsset sda))
        {
            gameObject.SetActive(false);
            return;
        }
        
        gameObject.SetActive(true);

        skeleton.skeletonDataAsset = sda;
        skeleton.Initialize(true);

        Spine.Animation idle = skeleton.Skeleton.Data.FindAnimation(ANIM_IDLE);
        if (idle != null) skeleton.AnimationState.SetAnimation(0, idle, false);

        skeleton.timeScale = 0f;
        skeleton.color = hidden ? Color.black : Color.white;

        questionMark.enabled = hidden;
        body.localScale = Vector3.zero;
    }

    public Tween PopIn(float delay)
    {
        body.DOKill();
        Sequence seq = DOTween.Sequence().SetTarget(body).SetUpdate(true).SetLink(gameObject)
            .AppendInterval(delay)
            .Append(body.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
        
        return seq;
    }
}
