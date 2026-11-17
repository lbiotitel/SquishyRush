using System;
using System.Collections.Generic;

/// <summary>
/// 재능을 보유하는 인벤토리
/// </summary>
public class PerkInventory
{
    private readonly List<PerkDataBase> perkItems = new();
    
    public IReadOnlyList<PerkDataBase> PerkItems => perkItems;
    public int Count => perkItems.Count;

    //인벤토리가 바뀔때마다 발행
    public event Action OnChanged;

    /// <summary>
    /// 인덱스의 재능 데이터를 가져옵니다. 범위를 벗어나면 null을 반환합니다.
    /// 뷰가 빈 칸을 그릴 때도 이 null을 그대로 사용하므로 예외를 던지지 않습니다.
    /// </summary>
    public PerkDataBase Get(int index)
        => index >= 0 && index < perkItems.Count ? perkItems[index] : null;

    /// <summary>
    /// 재능 하나를 맨 뒤에 추가합니다. (재능 선택, 구매)
    /// 맨 뒤에만 붙기 때문에 기존 항목의 인덱스는 바뀌지 않습니다.
    /// </summary>
    public void Add(PerkDataBase data)
    {
        if (data == null) return;
        
        perkItems.Add(data);
        Logger.Log($"[PerkInventory] {data.PerkId} 추가 (총 {perkItems.Count})");
        OnChanged?.Invoke();
    }

    /// <summary>
    /// 여러 개를 한 번에 추가합니다. (합성 초과분 반환, 판매 반환 등)
    /// 하나씩 Add하면 뷰가 개수만큼 다시 그려지므로 이벤트를 한 번만 발행합니다.
    /// </summary>
    public void AddRange(IEnumerable<PerkDataBase> datas)
    {
        if (datas == null) return;

        int before = perkItems.Count;
        foreach(PerkDataBase data in datas)
            if(data != null) perkItems.Add(data);

        if (perkItems.Count == before) return;
        
        Logger.Log($"[PerkInventory] {perkItems.Count - before}개 추가 (총 {perkItems.Count})");
        OnChanged?.Invoke();
    }

    /// <summary>
    /// 인덱스의 재능을 제거합니다. (유닛에게 장착했을 때)
    /// 뒤 항목이 한 칸씩 당겨지므로, 호출 후에는 이전에 들고 있던 인덱스를 다시 쓰면 안 됩니다.
    /// </summary>
    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= perkItems.Count) return false;

        PerkDataBase removed = perkItems[index];

        perkItems.RemoveAt(index);
        Logger.Log($"[PerkInventory] {removed.PerkId} 제거 (남은 {perkItems.Count})");
        
        OnChanged?.Invoke();
        return true;
    }
}
