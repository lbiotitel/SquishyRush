using System;


public class UserLevelData
{
    public UserLevelData(int requiredExp)
    {
        RequiredExp = requiredExp;
    }

    public int RequiredExp { get; private set; }
}

//유저 레벨 차트 데이터 입니다.
[Serializable]
public class UserLevelChartData : IChartData, IDomainConvertible<UserLevelData>
{
    public string Key { get; set; }
    public string RequiredExp { get; set; }


    public UserLevelData ToDomainData()
    {
        int requiredExp = int.TryParse(RequiredExp, out int val) ? val : 0;
        return new UserLevelData(requiredExp);
    }
}