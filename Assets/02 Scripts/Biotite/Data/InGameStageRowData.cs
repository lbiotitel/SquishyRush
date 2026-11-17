using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ERoundType
{
    Monster,
    EliteMonster,
    Boss,
    PerkShop
}


[Serializable]
public struct InGameStageRowUnitData
{
    public string UnitId;
    public int Level;
    public int Star;
}

[CreateAssetMenu(fileName = "InGameStageRowData", menuName = "ScriptableObjects/InGameStageRowData")]
public class InGameStageRowData : ScriptableObject
{
    [SerializeField] private List<InGameStageRowUnitData> unitRowDatas = new List<InGameStageRowUnitData>();
    [SerializeField] private ERoundType roundType;

    [System.Serializable]
    public struct GridRow
    {
        public bool[] cols;
    }

    [HideInInspector]
    public GridRow[] grid = new GridRow[2]
    {
        new GridRow { cols = new bool[3] },
        new GridRow { cols = new bool[3] }
    };
    
    public List<InGameStageRowUnitData> UnitRowDatas
    {
        get => unitRowDatas;
        set => unitRowDatas = value;
    }

    public ERoundType RoundType
    {
        get => roundType;
        set => roundType = value;
    }
    
    public bool IsPerkShop => roundType == ERoundType.PerkShop;
}