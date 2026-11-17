using UnityEngine;
using UnityEngine.UI;



public enum EStageType
{
    Forest,
    Ocean,
    Hell,
    Temple,
    Night,
    DemonCastle,
    Last
}

//로비의 스테이지 데이터 입니다.
[CreateAssetMenu]
public class LobbyStageData : ScriptableObject
{
    [SerializeField] private string stageNameKey;
    [SerializeField] private Sprite stageMainImage;
    [SerializeField] private EStageType stageType;
    [SerializeField] private Color stageMainColor;
    [SerializeField] private int stageLevel;
    [SerializeField] private GameObject stageUIPrefab;


    public string StageNameKey => stageNameKey;
    public EStageType StageType => stageType;
    public Color StageMainColor => stageMainColor;
    public int StageLevel => stageLevel;
    public GameObject StageUIPrefab => stageUIPrefab;
    public Sprite StageMainImage => stageMainImage;
}
