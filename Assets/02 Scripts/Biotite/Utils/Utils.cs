using Cysharp.Threading.Tasks;
using System.Text.RegularExpressions;
using BackEnd;
using UnityEngine;



//유틸에 관련된 메서드의 집합입니다.
public static class Utils 
{

    /// <summary>
    /// 정규 표현식을 사용해 닉네임의 유효성을 체크합니다.
    /// </summary>
    private static readonly Regex NicknameRegex = new Regex(
        @"^(?!.*(.)\1{2,})(?!(?:.*[0-9]){4,})[가-힣a-zA-Z][가-힣a-zA-Z0-9]{1,7}\z",
        RegexOptions.CultureInvariant);
    public static bool CheckNameIsValidate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return NicknameRegex.IsMatch(name);
    }



    /// <summary>
    /// 오버레이 UI를 띄운 상태로 반환값이 있는 UniTask 작업을 대기합니다.
    /// </summary>
    public static async UniTask<T> WaitForAwaitMethodWithOverlayUI<T>(UniTask<T> awaitMethod)
    {
        BaseUI wattingUI = GlobalManagers.UI.PopUpContainer.OpenUI<WattingUI>(new BaseUIData());
        try
        {
            T result = await awaitMethod;
            return result;
        }
        finally
        {
            GlobalManagers.UI.PopUpContainer.CloseUI(wattingUI);
        }
    }

    /// <summary>
    /// 오버레이 UI를 띄운 상태로 반환값이 없는 UniTask 작업을 대기합니다. (오버로딩)
    /// </summary>
    public static async UniTask WaitForAwaitMethodWithOverlayUI(UniTask awaitMethod)
    {
        BaseUI waitingUI = GlobalManagers.UI.PopUpContainer.OpenUI<WattingUI>(new BaseUIData());

        try
        {
            await awaitMethod;
        }
        finally
        {
            GlobalManagers.UI.PopUpContainer.CloseUI(waitingUI);
        }
    }
    
    
    /// <summary>
    /// 스탯에 대한 포맷팅을 수행합니다.
    /// </summary>
    /// <param name="baseStat">기본 값</param>
    /// <param name="bonus">보너스 값</param>
    /// <param name="suffix">접미사</param>
    /// <returns></returns>
    public static string FormatStatWithBonus(float baseStat, float bonus, string suffix = "")
    {
        if (bonus <= 0f) return $"{baseStat}{suffix}";
        return $"{baseStat}(+<color=green>{bonus}{suffix}</color>)";
    }
    

}
