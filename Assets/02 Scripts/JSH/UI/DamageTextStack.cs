using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛 하나에 뜨는 데미지 텍스트들을 세로로 쌓아 관리합니다. BattleUnitView가 하나씩 들고 있습니다.
///
/// 규칙
///  1. 새 텍스트는 항상 맨 아래(0번 칸)에 뜹니다. 먼저 뜬 텍스트는 한 칸씩 위로 밀립니다.
///  2. 칸 간격 = textHeight * stepRatio.
///     stepRatio가 1보다 작으면 자기 텍스트끼리 일부 겹칩니다. (0.65면 35% 겹침)
///     겹치는 부분은 새 텍스트가 앞에 그려지므로, 오래된 숫자는 윗부분만 보이게 됩니다.
///  3. 칸 위치가 maxHeight를 넘는 텍스트는 Eject로 빠르게 사라집니다. (오래된 것 밀어내기)
///
/// maxHeight 정하는 법
///   윗줄 적의 텍스트와 겹치지 않으려면  maxHeight ≤ 위아래 줄 간격 - textHeight  이어야 합니다.
///   현재 카메라 기준 줄 간격이 약 1.93이고 textHeight가 0.6이면 안전 최대값은 약 1.33입니다.
///   여유를 두고 1.2로 잡았습니다. textHeight를 실측값으로 바꾸면 maxHeight도 같이 확인하세요.
///   BattleUnitView를 선택하면 씬 뷰에 스택 영역이 박스로 표시됩니다.
///
/// 풀 재사용 대비
///   텍스트를 넣을 때 SpawnId를 같이 저장해 둡니다. 텍스트가 풀로 돌아갔다가 다른 유닛에게 재사용되면
///   SpawnId가 달라지므로, 이 스택은 그 텍스트를 더 이상 건드리지 않습니다.
/// </summary>
[Serializable]
public class DamageTextStack
{
    [Tooltip("숫자 한 줄의 월드 높이. DamageText 프리팹의 ⋮ > '텍스트 높이 측정'으로 잴 수 있습니다.")] 
    [SerializeField] private float textHeight = 0.6f;

    [Tooltip("칸 간격 = 텍스트 높이 × 이 값. 1이면 딱 붙고, 작을수록 많이 겹칩니다.")]
    [Range(0.3f, 1f)] 
    [SerializeField] private float stepRatio = 0.65f;

    [Tooltip("맨 아래 칸에서 맨 위 칸까지의 최대 높이. 이보다 위로 밀리는 텍스트는 사라집니다.")]
    [SerializeField] private float maxHeight = 1.2f;

    [Tooltip("한 칸 밀려 올라가는 시간 (초)")]
    [SerializeField] private float slideDuration = 0.1f;

    private struct Entry
    {
        public DamageText Text;
        public int SpawnId;

        public bool IsValid =>
            Text != null
            && Text.gameObject.activeSelf
            && Text.SpawnId == SpawnId
            && !Text.IsEjecting;
    }

    // 0번이 가장 최신(맨 아래). 직렬화 대상이 아니라 런타임에만 씁니다.
    [NonSerialized] private List<Entry> entries;
    
    public float TextHeight => textHeight;
    public float MaxHeight => maxHeight;
    public float Step => textHeight * stepRatio;

    
    /// <summary>
    /// 막 띄운 텍스트를 맨 아래 칸에 넣고, 기존 텍스트를 한 칸씩 위로 밉니다.
    /// 텍스트는 ShowNumber/ShowMessage를 먼저 호출한 상태여야 합니다. (SpawnId가 확정된 뒤)
    /// </summary>
    public void Push(DamageText text)
    {
        if (text == null) return;
        entries ??= new List<Entry>(8);

        RemoveInvalid();
        entries.Insert(0, new Entry { Text = text, SpawnId = text.SpawnId });
        Relayout();
    }

    /// <summary>수명이 끝났거나 다른 유닛에게 재사용된 텍스트를 목록에서 뺍니다.</summary>
    private void RemoveInvalid()
    {
        for(int i = entries.Count - 1; i >= 0; i--)
            if(!entries[i].IsValid) entries.RemoveAt(i);
    }

    private void Relayout()
    {
        float step = Step;

        for (int i = 0; i < entries.Count; i++)
        {
            float y = i * step;

            // 최대 높이를 넘은 칸부터 위는 전부 밀어냅니다.
            // 위로 더 올라가면 윗줄 적의 영역을 침범하므로, 한계 근처까지만 살짝 밀고 빠르게 사라지게 합니다.
            if (y > maxHeight + 0.0001f)
            {
                float ejectY = maxHeight + step * 0.3f;
                for (int k = entries.Count - 1; k >= i; k--)
                {
                    DamageText t = entries[k].Text;
                    t.SetStackOffset(ejectY, slideDuration);
                    t.Eject();
                    entries.RemoveAt(k);
                }

                break;
            }

            // 0번(방금 뜬 텍스트)은 제자리에 바로 두고, 나머지는 미끄러지듯 한 칸 올립니다.
            entries[i].Text.SetStackOffset(y, i == 0 ? 0f : slideDuration);
        }
    }
}
