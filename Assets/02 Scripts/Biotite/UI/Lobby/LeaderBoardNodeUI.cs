using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

// 랭킹의 단위 UI를 정의합니다.
public class LeaderBoardNodeUI : MonoBehaviour
{
    [Header("Image")]
    [SerializeField] private Image profileImage;
    [SerializeField] private Image tierImage;

    [Header("Text")]
    [SerializeField] private TMP_Text userNameText;
    [SerializeField] private TMP_Text userRpText;
    [SerializeField] private TMP_Text userRankText;

    private SpriteAtlas profileImageAtlas;

    private void Awake()
    {
        InitAtlas();
    }

    private void InitAtlas()
    {
        if (profileImageAtlas == null)
        {
            if (!GlobalManagers.Resource.TryGetResource<SpriteAtlas>(GlobalDefines.ResourceKey.Atlas.PROFILE_IMAGE_ATLAS,
                    out SpriteAtlas atlas))
            {
                Logger.LogError($"{GlobalDefines.ResourceKey.Atlas.PROFILE_IMAGE_ATLAS}에 해당하는 아틀라스가 없습니다.");
                return;
            }

            profileImageAtlas = atlas;
        }
    }

    public void OpenUI(UserRankData userRankData)
    {
        InitAtlas(); 
        
        Sprite loadedSprite = profileImageAtlas != null ? profileImageAtlas.GetSprite(userRankData.ProfileImageId) : null;

        if (loadedSprite != null)
            profileImage.sprite = loadedSprite;
        
        userRankText.text = userRankData.UserRank.ToString();
        userNameText.text = userRankData.UserName;
        userRpText.text = userRankData.UserRp.ToString();
        tierImage.color = GlobalDefines.TierRP.GetTierColor(userRankData.UserRp);
        gameObject.SetActive(true);
    }

    
    public void CloseUI() 
        => gameObject.SetActive(false);
}