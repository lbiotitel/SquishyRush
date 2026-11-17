using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class SynergyToolTipUIData : BaseUIData
{
    public Vector3 OpenPos { get; set; } = Vector3.zero;
    public SynergyStatus Status { get; set; }
}


//시너지 에 대한 툴팁입니다.
public class SynergyToolTipUI : BaseUI
{
    [Header("Header")] 
    [SerializeField] private Image synergyIcon;
    [SerializeField] private TMP_Text synergyNameText;
    
    [Header("Tiers")]
    [SerializeField] private TMP_Text synergyDescText;
    [SerializeField] private Color activeTierColor = new Color(0.3f, 1f, 0.3f);
    [SerializeField] private Color inactiveTierColor = new Color(0.55f, 0.55f, 0.55f);

    [SerializeField] private RectTransform tierRowRoot;
    [SerializeField] private SynergyTierRowView tierRowPrefab;
    
    [Header("Units")] 
    [SerializeField] private RectTransform unitIconRoot;
    [SerializeField] private SynergyUnitIconView unitIconPrefab;
    [SerializeField] private Color notOnBoardColor = new(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color[] rarityFrameColors;
    
    [Header("Layout")]
    [SerializeField] private float screenPadding = 8f;
    
    private const string TABLE = Localization.LocalizationTable.Synergy;
    
    private RectTransform rectTransform;
    private readonly StringBuilder sb = new(256);
    private readonly List<SynergyUnitIconView> unitIcons = new();
    private readonly List<SynergyTierRowView> tierRows = new();
    private readonly HashSet<string> onBoardIds = new();
    private readonly Vector3[] corners = new Vector3[4];
    private string activeHex, inactiveHex;
    
    public override void Initialize(Transform anchor)
    {
        rectTransform = transform.GetComponent<RectTransform>();
        float originWidth = rectTransform.rect.width;
        base.Initialize(anchor);
        rectTransform.sizeDelta = new Vector2(originWidth, 0);

        activeHex = "#" + ColorUtility.ToHtmlStringRGB(activeTierColor);
        inactiveHex = "#" + ColorUtility.ToHtmlStringRGB(inactiveTierColor);
    }


    public override void SetInfo(BaseUIData data)
    {
        base.SetInfo(data);
        if (data is not SynergyToolTipUIData d) return;

        SynergyStatus status = d.Status;
        SynergyData head = status.Tiers[0];

        synergyIcon.sprite = head.DisplayIcon;
        synergyNameText.text = Localization.GetLocaliationText(TABLE, head.SynergyNameKey);
        
        if(tierRowPrefab != null) BuildTierRows(status);
        else BuildTierText(status);

        BulidUnitIcons(head);

        transform.position = d.OpenPos;
        ClampToScreen();
        
    }
    

    private void BuildTierText(in SynergyStatus status)
    {
        sb.Clear();
        for (int i = 0; i < status.Tiers.Count; i++)
        {
            SynergyData tier = status.Tiers[i];
            bool isOn = i == status.ActiveTier - 1;

            if (i > 0) sb.Append('\n');
            sb.Append("<color=").Append(isOn ? activeHex : inactiveHex).Append('>')
                .Append('(').Append(tier.RequiredCount).Append(")")
                .Append(Localization.GetLocaliationText(TABLE, tier.SynergyDescKey))
                .Append("</color>");
        }
        synergyDescText.SetText(sb);
    }

    /// <summary>단계마다 한 줄씩. 켜진 단계 판정은 BuildTierText와 같습니다.</summary>
    private void BuildTierRows(in SynergyStatus status)
    {
        if(synergyDescText != null) synergyDescText.gameObject.SetActive(false);

        int count = status.Tiers.Count;
        while(tierRows.Count < count)
            tierRows.Add(Instantiate(tierRowPrefab, tierRowRoot));

        for (int i = 0; i < tierRows.Count; i++)
        {
            bool used = i < count;
            tierRows[i].gameObject.SetActive(used);
            if (!used) continue;

            SynergyData tier = status.Tiers[i];
            tierRows[i].Set(tier.RequiredCount,
                Localization.GetLocaliationText(TABLE, tier.SynergyDescKey),
                i == status.ActiveTier - 1);
        }
    }

    private void BulidUnitIcons(SynergyData head)
    {
        onBoardIds.Clear();
        List<BattleUnit> placed = InGameManagers.Board.GetBattleUnits();
        for (int i = 0; i < placed.Count; i++)
        {
            UnitData u = placed[i]?.CurrentUnitData;
            if (u != null) onBoardIds.Add(u.UnitId);
        }

        IReadOnlyList<UnitData> pool = InGameManagers.Run.OwnedUnits;
        int used = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            UnitData unit = pool[i];
            if (!head.Matches(unit)) continue;
            
            if(used == unitIcons.Count)
                unitIcons.Add(Instantiate(unitIconPrefab, unitIconRoot));

            SynergyUnitIconView view = unitIcons[used++];
            view.gameObject.SetActive(true);
            
            Color frame = GlobalDefines.Rarity.GetRarityPastelColor(unit.UnitRarity);
           view.Set(unit, frame, onBoardIds.Contains(unit.UnitId), notOnBoardColor);
        }
        
        for(int i = used; i < unitIcons.Count; i++)
            unitIcons[i].gameObject.SetActive(false);
    }
    
    
    /// <summary>
    /// ui가 화면 밖으로 안나가게 보정합니다.
    /// </summary>
    private void ClampToScreen()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        rectTransform.GetWorldCorners(corners);
        
        float screenW = Screen.width;
        float screenH = Screen.height;

        Vector3 pos = rectTransform.position;
        
        float minX = corners[0].x;
        float maxX = corners[2].x;
        float minY = corners[0].y;
        float maxY = corners[2].y;

        float offsetX = 0f;
        float offsetY = 0f;

        if (minX < screenPadding)
            offsetX = screenPadding - minX;
        else if (maxX > screenW - screenPadding)
            offsetX = (screenW - screenPadding) - maxX;

        if (minY < screenPadding)
            offsetY = screenPadding - minY;
        else if (maxY > screenH - screenPadding)
            offsetY = (screenH - screenPadding) - maxY;

        pos.x += offsetX;
        pos.y += offsetY;

        rectTransform.position = pos;
        //Logger.LogWarning($"제어 {rectTransform.position}");
    }
}
