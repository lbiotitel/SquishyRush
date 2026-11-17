using System.Collections.Generic;
using UnityEngine;


//스테이지의 row 데이터들을 담아놓는 컨테이너 입니다.
[CreateAssetMenu(fileName = "InGameStageDataContainer", menuName = "ScriptableObjects/InGameStageDataContainer")]
public class InGameStageDataContainer : ScriptableObject
{
    [SerializeField] private string stageName;
    [SerializeField] private Color stageColor = Color.white;
    [SerializeField] private GameObject stageMapPrefab;
    [SerializeField] private List<InGameStageRowData> rows;
    
    public string  StageName => stageName;
    public Color StageColor => stageColor;
    public List<InGameStageRowData> Rows => rows;
    public GameObject StageMapPrefab => stageMapPrefab;

    public int FinalRound => rows?.Count ?? 0;

    public ERoundType GetRoundType(int round)
    {
        int i = round - 1;
        if (rows == null || i < 0 || i >= rows.Count || rows[i] == null) return ERoundType.Monster;
        return rows[i].RoundType;
    }

    public int GetDisplayRound(int round)
    {
        int count = 0;
        for (int r = 1; r <= round && r <= FinalRound; r++)
            if (!IsPerkShopRound(r)) count++;
        return count;
    }

    public int TotalDisplayRound => GetDisplayRound(FinalRound);
    public bool IsPerkShopRound(int round) => GetRoundType(round) == ERoundType.PerkShop;
}
