using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;


//프로필 이미지를 바꾸는 팝업  창입니다.
public class ProfileImageChangeUI : BaseUI
{
    [Header("Button")]
    [SerializeField] private AnimButton changeButton;
    [SerializeField] private AnimButton closeButton;

    [Header("Transform")] 
    [SerializeField] private Transform content; 
        
    private Sprite[] profileImages;
    private string currentProfileImageID;

    private ProfileService profileService;
    
    private List<ProfileImageCellView>  profileImageCellViews;
    private GameObject profileImageCellViewPrefab;
    
    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);

        if (!GlobalManagers.Resource.TryGetResource<GameObject>(GlobalDefines.ResourceKey.UI.PROFILE_IMAGE_CELL_VIEW_PREFAB,
                out GameObject prefab))
        {
            Logger.LogError($"{GlobalDefines.ResourceKey.UI.PROFILE_IMAGE_CELL_VIEW_PREFAB}에 해당하는 프리팹이 없습니다.");
            return;
        }

        profileImageCellViewPrefab = prefab;
        
        profileService = GlobalManagers.Service.GetService<ProfileService>();
        currentProfileImageID = profileService.UserProfileImageID;

        if (!GlobalManagers.Resource.TryGetResource<SpriteAtlas>(GlobalDefines.ResourceKey.Atlas.PROFILE_IMAGE_ATLAS,
                out SpriteAtlas atlas))
        {
            Logger.LogError($"{GlobalDefines.ResourceKey.Atlas.PROFILE_IMAGE_ATLAS}에 해당하는 아틀라스가 없습니다.");
            return;
        }

        SpriteAtlas spriteAtlas = atlas;
        profileImages = new Sprite[spriteAtlas.spriteCount];
        spriteAtlas.GetSprites(profileImages);

        BindEvents();
        
        CreateProfileImageCellView();
    }


    private void BindEvents()
    {
        changeButton.OnClicked += OnClickedChangeButton;
        closeButton.OnClicked += OnClickedCloseButton;
    }


    private void OnDestroy()
    {
        changeButton.OnClicked -= OnClickedChangeButton;
        closeButton.OnClicked -= OnClickedCloseButton;
        
        foreach(var cellView in profileImageCellViews)
            cellView.OnClicked -= OnClickedProfileImageCellView;
    }

    

    /// <summary>
    /// 이미지를 구성하기 위한 cellview를 미리 생성합니다.
    /// </summary>
    private void CreateProfileImageCellView()
    {
        profileImageCellViews = new List<ProfileImageCellView>();
        for (int i = 0; i < profileImages.Length; i++)
        {
            GameObject go = Instantiate(profileImageCellViewPrefab, content);
            ProfileImageCellView cell = go.GetComponent<ProfileImageCellView>();
            cell.Initialize(profileImages[i]);
            cell.OnClicked += OnClickedProfileImageCellView;
            
            if(currentProfileImageID == cell.ProfileImageID)
                cell.SetCheckObject(true);
            
            profileImageCellViews.Add(cell);
        }
    }


    /// <summary>
    /// cell view를 클릭할시 클릭한 cell view의 프로필이미지 id를 저장합니다.
    /// </summary>
    /// <param name="profileImageCellView"></param>
    private void OnClickedProfileImageCellView(ProfileImageCellView profileImageCellView)
    {
        foreach(var  cellView in profileImageCellViews)
            cellView.SetCheckObject(false);
        
        currentProfileImageID = profileImageCellView.ProfileImageID;
        profileImageCellView.SetCheckObject(true);
    }


    private void OnClickedCloseButton()
        => CloseUI();
    private void OnClickedChangeButton()
        => ChangeProfileImage().Forget();

    
    /// <summary>
    /// 선택한 이미지 id가 현재 유저에 저장된 이미지 id와 다르면 바꿉니다.
    /// </summary>
    private async UniTask ChangeProfileImage()
    {
        if (currentProfileImageID != profileService.UserProfileImageID)
        {
            bool res = await Utils.WaitForAwaitMethodWithOverlayUI(profileService.SetProfileImage(currentProfileImageID));

            if (res)
            {
                Logger.Log("이미지 변경 성공");
            }
            else
            {
                Logger.Log("이미지 변경 실패");
            }
            
            CloseUI();
        }
    }
    
    

}
