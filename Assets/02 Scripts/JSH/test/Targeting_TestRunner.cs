using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 진영별 타겟팅 검증.
/// 보드 좌표는 양쪽 다 [0][1][2] / [3][4][5] 이고, 전열만 반대입니다.
///
///        아군                    적
///     [0][1][2]      ←→      [0][1][2]
///     [3][4][5]              [3][4][5]
///           ↑ col2=전열       ↑ col0=전열
///
/// RuntimeId 로 슬롯을 식별합니다. 아군은 100+슬롯, 적은 200+슬롯.
/// </summary>
public class Targeting_TestRunner : MonoBehaviour
{
    private BoardModel ally;
    private BoardModel enemy;
    private System.Random rng;

    private readonly List<BattleUnit> result = new();

    private void Start()
    {
        rng = new System.Random(12345);   // 고정 시드. 매번 같은 결과가 나와야 합니다.

        ally  = MakeBoard(100, new[] { true, true, true, true, true, true });
        enemy = MakeBoard(200, new[] { true, true, true, true, true, true });

        Debug.Log("========== 꽉 찬 보드 ==========");
        RunBothDirections();

        // 적 전열(col0 = 슬롯 0,3)을 비워 스캔이 다음 열로 넘어가는지 봅니다.
        enemy = MakeBoard(200, new[] { false, true, true, false, true, true });
        // 아군 전열(col2 = 슬롯 2,5)도 비웁니다.
        ally  = MakeBoard(100, new[] { true, true, false, true, true, false });

        Debug.Log("========== 양쪽 전열 비움 ==========");
        RunBothDirections();
    }

    private void RunBothDirections()
    {
        BattleUnit allyCaster  = ally.Get(new SlotIndex(EZone.Battle, 0));
        BattleUnit enemyCaster = enemy.Get(new SlotIndex(EZone.Battle, 2));

        Debug.Log($"[아군 보드]\n{Dump(ally)}[적 보드]\n{Dump(enemy)}");

        // 아군이 적을 침 → 대상 보드는 적 보드 → boardIsAlly = false
        Section("아군 → 적", allyCaster, enemy, boardIsAlly: false);

        // 적이 아군을 침 → 대상 보드는 아군 보드 → boardIsAlly = true
        Section("적 → 아군", enemyCaster, ally, boardIsAlly: true);
    }

    private void Section(string label, BattleUnit caster, BoardModel board, bool boardIsAlly)
    {
        Debug.Log($"----- {label} (boardIsAlly={boardIsAlly}) -----");

        // Front / Back 은 여러 번 돌려 위아래가 골고루 뽑히는지 봅니다.
        for (int i = 0; i < 4; i++)
            LogSelect(ETargetSelectType.Front, caster, board, boardIsAlly);

        LogSelect(ETargetSelectType.Back, caster, board, boardIsAlly);
        LogSelect(ETargetSelectType.Random, caster, board, boardIsAlly);

        // Square 는 앵커를 고정해 flipX 방향만 봅니다.
        foreach (int anchorIdx in new[] { 0, 2 })
        {
            var anchor = new SlotIndex(EZone.Battle, anchorIdx);
            if (board.Get(anchor) == null) continue;

            AreaResolver.Resolve(EAreaType.Square, anchor, boardIsAlly, board, result);
            Debug.Log($"  Square 앵커{anchorIdx} → [{Ids()}]");
        }

        // 대칭 범위는 flipX 를 바꿔도 같아야 합니다.
        var mid = new SlotIndex(EZone.Battle, 1);
        if (board.Get(mid) != null)
        {
            AreaResolver.Resolve(EAreaType.S_Cross, mid, false, board, result);
            string a = Ids();
            AreaResolver.Resolve(EAreaType.S_Cross, mid, true, board, result);
            string b = Ids();
            Debug.Log($"  S_Cross 앵커1  flipX=F [{a}] / flipX=T [{b}]  {(a == b ? "일치" : "불일치!")}");
        }
    }

    private void LogSelect(ETargetSelectType type, BattleUnit caster, BoardModel board, bool boardIsAlly)
    {
        if (!TargetSelector.TrySelect(type, caster, board, boardIsAlly, rng, out var anchor))
        {
            Debug.Log($"  {type,-6} → 대상 없음");
            return;
        }

        var unit = board.Get(anchor);
        Debug.Log($"  {type,-6} → 슬롯{anchor.Index} (id {unit.RuntimeId})");
    }

    // ── 준비 ────────────────────────────────────────────────

    private BoardModel MakeBoard(int idBase, bool[] occupied)
    {
        var b = new BoardModel(2, 3, 0);
        for (int i = 0; i < occupied.Length; i++)
        {
            if (!occupied[i]) continue;
            var u = new BattleUnit();
            u.Init(idBase + i, null, 1, new UnitBaseStat(10f, 100f, 0f, 100f, 1f, 100f, 10f), idBase == 100);
            b.Place(new SlotIndex(EZone.Battle, i), u);
        }
        return b;
    }

    private string Ids()
    {
        if (result.Count == 0) return "없음";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < result.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(result[i].RuntimeId);
        }
        return sb.ToString();
    }

    private string Dump(BoardModel b)
    {
        var sb = new System.Text.StringBuilder();
        for (int r = 0; r < b.BattleRows; r++)
        {
            for (int c = 0; c < b.BattleCols; c++)
            {
                var u = b.Get(new SlotIndex(EZone.Battle, b.ToIndex(r, c)));
                sb.Append(u == null ? "  .  " : $" {u.RuntimeId} ");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}