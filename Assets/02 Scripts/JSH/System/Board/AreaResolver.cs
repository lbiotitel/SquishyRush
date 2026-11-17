using System.Collections.Generic;
using UnityEngine;

public static class AreaResolver
{
    // 방향 상수
    private static readonly Vector2Int[] OffsetSide = { new(0,1), new(0, -1) };
    private static readonly Vector2Int[] OffsetSCross = { new(0,0), new(0,1), new(0, -1), new(-1, 0), new(1, 0) };

    public static void Resolve(EAreaType area, SlotIndex anchor, bool flipX,
        BoardModel board, List<BattleUnit> result)
    {
        result.Clear();

        switch (area)
        {
            case EAreaType.All:
                CollectAll(board, result);
                return;
            case EAreaType.Row:
                CollectAxis(board, anchor, fixRow: true, result);
                return;
            case EAreaType.Column:
                CollectAxis(board, anchor, fixRow: false, result);
                return;
            case EAreaType.Single:
                AddSlot(board, anchor.Index, result);
                return;
            case EAreaType.Square:
                CollectSquare(board, anchor, flipX, result);
                return;
            case EAreaType.S_Cross:
                CollectOffsets(OffsetSCross, board, anchor, result);
                return;
            case EAreaType.Side:
                CollectOffsets(OffsetSide, board, anchor, result);
                if(result.Count == 0) AddSlot(board, anchor.Index, result);
                return;
        }
    }

    private static bool AddSlot(BoardModel board, int index, List<BattleUnit> result)
    {
        BattleUnit unit = board.Get(new SlotIndex(EZone.Battle, index));
        if (unit == null || !unit.IsAlive) return false;
        
        if(!result.Contains(unit)) result.Add(unit);
        return true;
    }
    
    //- 절대 축-
    /// <summary>
    /// fixRow = true이면 가로 줄 전체, false이면 세로 줄 전체
    /// </summary>
    private static void CollectAxis(BoardModel board, SlotIndex anchor, bool fixRow,
        List<BattleUnit> result)
    {
        board.ToRowCol(anchor.Index, out int row, out int col);

        int count = fixRow ? board.BattleCols : board.BattleRows;
        for (int i = 0; i < count; i++)
        {
            int idx = fixRow ? board.ToIndex(row, i) : board.ToIndex(i, col);
            AddSlot(board, idx, result);
        }
    }
    
    //- 상대 좌표-

    private static void CollectSquare(BoardModel board, SlotIndex anchor, bool flipX,
        List<BattleUnit> result)
    {
        board.ToRowCol(anchor.Index, out int row, out int col);
        row = Mathf.Clamp(row, 0, Mathf.Max(0, board.BattleRows - 2));

        int dc = flipX ? -1 : 1;

        for (int r = row; r <= row + 1; r++)
        {
            for (int i = 0; i <= 1; i++)
            {
                int c = col + dc * i;
                if (r < 0 || r >= board.BattleRows) continue;
                if (c < 0 || c >= board.BattleCols) continue;
                AddSlot(board, board.ToIndex(r, c), result);
            }
        }
    }
    
    private static void CollectOffsets(Vector2Int[] offsets, BoardModel board, SlotIndex anchor,
        List<BattleUnit> result)
    {
        board.ToRowCol(anchor.Index, out int row, out int col);
        

        for (int i = 0; i < offsets.Length; i++)
        {
            int r = row + offsets[i].x;
            int c = col + offsets[i].y;
            
            if(r < 0 || r >= board.BattleRows) continue;
            if(c < 0 || c >= board.BattleCols) continue;
            
            AddSlot(board, board.ToIndex(r, c), result);
        }

    }

    //전체
    private static void CollectAll(BoardModel board, List<BattleUnit> result)
    {
        int total = board.BattleRows * board.BattleCols;
        for (int i = 0; i < total; i++)
        {
            AddSlot(board, i, result);
        }
    }
}


