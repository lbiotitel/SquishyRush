using UnityEngine;


//트리거 형태의 재능 데이터 입니다.
[CreateAssetMenu]
public class TriggerPerkData : PerkDataBase
{
    [Header("Trigger Perk")]
    [SerializeField] private ETriggerType startTrigger;
    [SerializeField] private ETriggerType endTrigger;

    [Tooltip("체크하면 시작 트리거가 올 때마다 효과를 다시 적용합니다. (~할 때마다)")]
    [SerializeField] private bool isRepeat;

    public bool IsRepeat => isRepeat;
    
    private void OnValidate()
    {
        perkType = EPerkType.Trigger;
    }
    
    
    public ETriggerType StartTrigger =>  startTrigger;
    public ETriggerType EndTrigger => endTrigger;
}
