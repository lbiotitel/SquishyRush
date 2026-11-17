using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;


//프로필 이미지 선택창에서 이미지들의 cell view 입니다
public class ProfileImageCellView : MonoBehaviour, IPointerClickHandler
{
    [Header("Image")]
    [SerializeField] private Image profileImage;
    
    [Header("Object")]
    [SerializeField] private GameObject checkObject;
    
    public Action<ProfileImageCellView> OnClicked;
    
    public string ProfileImageID { get; private set; }

    public void Initialize(Sprite profileImage)
    {
        this.profileImage.sprite = profileImage;
        ProfileImageID = profileImage.name.Replace("(Clone)", "");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClickedProfileImageCellView();
    }

    private void OnClickedProfileImageCellView()
    {
        OnClicked?.Invoke(this);
    }

    
    public void SetCheckObject(bool isChecked)
        => checkObject.SetActive(isChecked);

    public void OpenUI()
        => gameObject.SetActive(true);
    
    public void CloseUI()
        => gameObject.SetActive(false);
}
