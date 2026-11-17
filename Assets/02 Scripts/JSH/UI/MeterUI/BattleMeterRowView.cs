using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [미터기] 막대 한 줄의 뷰. 유닛 행과 시너지 행을 같은 프리팹으로 그립니다.
/// </summary>
public class BattleMeterRowView : MonoBehaviour
{
    [Header("아이콘")] 
    [SerializeField] private GameObject unitIconRoot;
    [SerializeField] private Image unitIcon;
    [SerializeField] private GameObject[] stars;
    [SerializeField] private GameObject synergyIconRoot;
    [SerializeField] private Image synergyIcon;

    [Header("텍스트")] 
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text valueText;

    [Header("막대 (뒤 -> 앞")] 
    [SerializeField] private Image fillBack;
    [SerializeField] private Image fillMid;
    [SerializeField] private Image fillFront;

    /// <summary>
    /// 한 줄을 그립니다.
    /// colors는 탭마다 다른 세 장의 색입니다(피해 탭: 흰/파랑/주황, 지원 탭: 하늘/초록/초록).
    /// max가 0이면(이 탭에 아무 값도 없음) 막대는 비고 숫자만 0으로 나옵니다.
    /// </summary>
    public void Bind(MeterRow row, EMeterTab tab, float max, in MeterBarColors colors)
    {
        gameObject.SetActive(true);
        
        BindIcon(row);

        nameText.text = row.Name;
        valueText.SetText("{0:0}", row.GetTotal(tab));
        
        row.GetFills(tab, max, out float back, out float mid, out float front);

        fillBack.color = colors.Back;
        fillMid.color = colors.Mid;
        fillFront.color = colors.Front;

        SetBarWidth(fillBack, back);
        SetBarWidth(fillMid, mid);
        SetBarWidth(fillFront, front);
    }

    /// <summary>
    /// 막대 한 장의 길이를 0~1 비율로 맞춥니다.
    /// 왼쪽 앵커는 0, 오른쪽 앵커를 ratio에 두고 여백을 0으로 만들어 Track 안에서 왼쪽부터 ratio만큼 차게 합니다.
    /// 0이면 아예 끕니다. 폭 0짜리 알약이 점처럼 남는 것을 막기 위해서입니다.
    /// 아주 작은 값이면 알약이 막대 높이보다 좁아지는데, Sliced 이미지는 테두리를 비율대로 줄여 작은 타원으로 그립니다.
    /// </summary>
    private static void SetBarWidth(Image image, float ratio)
    {
        ratio = Mathf.Clamp01(ratio);
        image.enabled = ratio > 0f;

        RectTransform rt = image.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(ratio, 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
    
    /// <summary>쓰지 않는 줄을 끕니다. 줄 수가 탭마다 달라서(시너지 유무) 풀에서 남는 줄이 생깁니다.</summary>
    public void Hide() => gameObject.SetActive(false);

    /// <summary>
    /// 유닛이면 둥근 초상화와 별을, 시너지면 네모 아이콘만 켭니다.
    /// </summary>
    private void BindIcon(MeterRow row)
    {
        unitIconRoot.SetActive(!row.IsSynergy);
        synergyIconRoot.SetActive(row.IsSynergy);

        if (row.IsSynergy)
        {
            synergyIcon.sprite = row.Icon;
            return;
        }

        unitIcon.sprite = row.Icon;
        for (int i = 0; i < stars.Length; i++)
            stars[i].SetActive(i < row.Star);
    }
}

/// <summary>[미터기] 막대 세 장의 색 묶음. 드로어가 탭에 맞춰 골라 넘깁니다.</summary>
public readonly struct MeterBarColors
{
    public readonly Color Back;
    public readonly Color Mid;
    public readonly Color Front;

    public MeterBarColors(Color back, Color mid, Color front)
    {
        Back = back;
        Mid = mid;
        Front = front;
    }
}
