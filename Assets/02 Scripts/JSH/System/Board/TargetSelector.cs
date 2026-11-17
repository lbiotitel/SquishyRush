using System;

public static class TargetSelector
{
    public static bool TrySelect(ETargetSelectType type, BattleUnit caster, BoardModel board,
        bool boardIsAlly, Random rng, out SlotIndex anchor)
    {
        int frontCol = boardIsAlly ? board.BattleCols - 1 : 0;
        int frontStep = boardIsAlly ? -1 : 1;
        
        switch (type)
        {
            case ETargetSelectType.Self:
                return board.TryFindSlot(caster, out anchor);
            case ETargetSelectType.Front:
                return ScanColumns(board, frontCol, frontStep, rng, out anchor);
            case ETargetSelectType.Back:
                return ScanColumns(board, frontCol + frontStep * (board.BattleCols - 1), -frontStep, rng, out anchor);
            case ETargetSelectType.Random:
                return PickAnyAlive(board, rng, out anchor);
            case ETargetSelectType.LowestHp:
                return PickLowestHpRatio(board, rng, out anchor);
        }

        anchor = default;
        return false;
    }

    /// <summary>
    /// 열을 step 방향으로 훓다가 생존자가 있는 첫 열에서 무작위 1명
    /// </summary>
    private static bool ScanColumns(BoardModel board, int startCol, int step, Random rng, out SlotIndex anchor)
    {
        anchor = default;

        for (int c = startCol; c >= 0 && c < board.BattleCols; c += step)
        {
            int found = 0;
            int picked = -1;

            for (int r = 0; r < board.BattleRows; r++)
            {
                int idx = board.ToIndex(r, c);
                BattleUnit unit = board.Get(new SlotIndex(EZone.Battle, idx));
                if (unit == null || !unit.IsAlive) continue;

                found++;
                if (rng.Next(found) == 0) picked = idx;
            }

            if (found == 0) continue;

            anchor = new SlotIndex(EZone.Battle, picked);
            return true;
        }

        return false;
    }

    private static bool PickAnyAlive(BoardModel board, Random rng, out SlotIndex anchor)
    {
        anchor = default;
        int total = board.BattleRows * board.BattleCols;
        int found = 0;
        int picked = -1;

        for (int i = 0; i < total; i++)
        {
            BattleUnit unit = board.Get(new SlotIndex(EZone.Battle, i));
            if (unit == null || !unit.IsAlive) continue;

            found++;
            if (rng.Next(found) == 0) picked = i;
        }

        if (found == 0) return false;
        
        anchor = new SlotIndex(EZone.Battle, picked);
        return true;
    }

    /// <summary>
    /// 체력 비율(CurrentHp / MaxHp)이 가장 낮은 생존 유닛. 힐러가 쓰는 기준점입니다.
    ///
    /// 절대값이 아니라 비율로 고르는 이유: 절대값이면 체력이 큰 탱커가 반피여도
    /// 거의 죽어가는 후열보다 후순위가 되고, 반대로 풀피 후열이 '가장 낮은 체력'으로 잡힙니다.
    /// 동률(전원 풀피 포함)이면 그중 무작위 1명입니다. 쉴드는 보지 않습니다.
    /// </summary>
    private static bool PickLowestHpRatio(BoardModel board, Random rng, out SlotIndex anchor)
    {
        const float EPS = 0.0001f;

        anchor = default;
        int total = board.BattleRows * board.BattleCols;
        float best = float.MaxValue;
        int found = 0;
        int picked = -1;

        for (int i = 0; i < total; i++)
        {
            BattleUnit unit = board.Get(new SlotIndex(EZone.Battle, i));
            if (unit == null || !unit.IsAlive || unit.MaxHp <= 0f) continue;

            float ratio = unit.CurrentHp / unit.MaxHp;

            if (ratio < best - EPS)
            {
                best = ratio;
                found = 1;
                picked = i;
            }
            else if (ratio <= best + EPS)
            {
                found++;
                if (rng.Next(found) == 0) picked = i;
            }
        }

        if (picked < 0) return false;

        anchor = new SlotIndex(EZone.Battle, picked);
        return true;
    }
}
