
using Cysharp.Threading.Tasks;
using BackEnd;


/// <summary>
/// 유저 서버 데이터들의 공통 인터페스 입니다.
/// </summary>
public interface IUserServerData 
{
    public string InDate { get; }
    bool IsDirty { get; } 
    public UniTask<bool> LoadDataAsync();
    public UniTask<bool> SaveDataAsync();
    public bool SaveData();
    public void SetDefaultData();
    public Param ToParam();
}
