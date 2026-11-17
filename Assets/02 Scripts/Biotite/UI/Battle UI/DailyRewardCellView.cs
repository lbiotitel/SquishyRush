using TMPro;
using UnityEngine;
using UnityEngine.UI;



//출석체크 에서 각각의 Day의 Node를 표시하는 UI 입니다.
public class DailyRewardCellView : MonoBehaviour
{
    [Header("Image")]
    [SerializeField] Image rewardImage;

    [Header("Text")]
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text rewardAmountText;

    [Header("Object")]
    [SerializeField] private GameObject checkObject;


    /// <summary>
    /// 출석체크 노드 ui를 초기화 합니다.
    /// </summary>
    /// <param name="rewardImage"></param>
    /// <param name="dayText"></param>
    /// <param name="rewardAmountText"></param>
    public void Initailize(Sprite rewardImage, string dayText, string rewardAmountText)
    {
        this.rewardImage.sprite = rewardImage;
        this.dayText.text = $"Day {dayText}";
        this.rewardAmountText.text = rewardAmountText;
        
    }

    /// <summary>ㅡ
    /// 출석체크 했다는 오브젝트를 켜고 끕니다.
    /// </summary>
    /// <param name="isClaimed"></param>
    public void SetCheckObject(bool isClaimed)
        => checkObject.SetActive(isClaimed);
}
