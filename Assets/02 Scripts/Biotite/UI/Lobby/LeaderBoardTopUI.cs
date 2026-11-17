using System;
using System.Collections;
using System.Collections.Generic;
using BackEnd.Quobject.EngineIoClientDotNet.Modules;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;


//탑3에 해당하는 유저들의 리더보드 ui 입니다.
public class LeaderBoardTopUI : MonoBehaviour
{
    [Header("Image")]
    [SerializeField] private Image profileImage;
    [SerializeField] private Image tierImage;
     
    [Header("Text")]
    [SerializeField] private TMP_Text userNameText;
    [SerializeField] private TMP_Text userRpText;
    
    [SerializeField] private CanvasGroup canvasGroup;
    

    
    public void OpenUI(UserRankData userRankData)
    {
        profileImage.sprite = GlobalManagers.Sprite.GetProfileImage(userRankData.ProfileImageId);
        
        canvasGroup.alpha = 1;
        userNameText.text = userRankData.UserName;
        userRpText.text = $"{userRankData.UserRp}";
        tierImage.color = GlobalDefines.TierRP.GetTierColor(userRankData.UserRp);
    }

    
    public void CloseUI()
        => canvasGroup.alpha = 0f;

}
