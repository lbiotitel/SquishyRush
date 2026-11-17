using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



//매일의 리워드 아이템을 그리는 cell view 입니다.
public class MailRewardItemCellView : MonoBehaviour
{
    [Header("Image")]
    [SerializeField] private Image itemImage;

    [Header("Amount")]
    [SerializeField] private TMP_Text amountText;


    /// <summary>
    /// 매일 아이템을 그립니다.
    /// </summary>
    /// <param name="itemID"></param>
    /// <param name="amount"></param>
    public void OpenUI(string itemID, int amount)
    {
        gameObject.SetActive(true);

        if (!GlobalManagers.Resource.TryGetResource<Sprite>($"{itemID}.sprite", out Sprite sprite))
        {
            Logger.LogError($"{itemID}.sprite 에 해당하는 sprite 가 없습니다.");
            return;
        }

        itemImage.sprite = sprite;
        amountText.text = $"{amount}";
    }


    public void CloseUI()
        => gameObject.SetActive(false);


}
