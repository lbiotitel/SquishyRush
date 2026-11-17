using System.Collections.Generic;
using UnityEngine;

public class AreaResolver_TestRunner : MonoBehaviour
{
    private BoardModel board;
    private readonly List<BattleUnit> result = new();
    private int nextId = 1;

    private void Start()
    {
        // 패턴: O=유닛, .=빈칸
        //  [0]O [1]O [2]O
        //  [3]. [4]O [5].
        RunCase("기본", new[] { true, true, true, false, true, false });

        //  [0]O [1]. [2]O      Front 스캔이 빈 칸을 건너뛰는지
        //  [3]O [4]. [5]O
        RunCase("가운데 열 비움", new[] { true, false, true, true, false, true });

        //  [0]O [1]. [2].      한 방향도 못 맞을 때 앵커 폴백
        //  [3]. [4]. [5].
        RunCase("혼자", new[] { true, false, false, false, false, false });
    }

    private void RunCase(string label, bool[] occupied)
    {
        board = new BoardModel(2, 3, 0);
        for (int i = 0; i < occupied.Length; i++)
            if (occupied[i]) board.Place(new SlotIndex(EZone.Battle, i), MakeUnit());

        Logger.Log($"===== {label} =====\n{DumpBoard()}");

        foreach (int anchorIdx in new[] { 0, 1, 4 })
        {
            var anchor = new SlotIndex(EZone.Battle, anchorIdx);
            foreach (EAreaType area in System.Enum.GetValues(typeof(EAreaType)))
            {
                AreaResolver.Resolve(area, anchor, false, board, result);
                Logger.Log($"앵커{anchorIdx} {area,-8} → [{string.Join(",", Ids())}] ({result.Count}칸)");
            }
        }
    }

    private BattleUnit MakeUnit()
    {
        var u = new BattleUnit();
        u.Init(nextId++, null, 1, new UnitBaseStat(10f, 100f, 0f, 100f, 1f, 100f, 10f), true);
        return u;
    }

    private IEnumerable<int> Ids()
    {
        foreach (var u in result) yield return u.RuntimeId;
    }

    private string DumpBoard()
    {
        var sb = new System.Text.StringBuilder();
        for (int r = 0; r < board.BattleRows; r++)
        {
            for (int c = 0; c < board.BattleCols; c++)
            {
                var u = board.Get(new SlotIndex(EZone.Battle, board.ToIndex(r, c)));
                sb.Append(u == null ? " .  " : $" {u.RuntimeId}  ");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}