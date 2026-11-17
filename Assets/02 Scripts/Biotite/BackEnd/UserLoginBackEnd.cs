
using BackEnd;
using Cysharp.Threading.Tasks;
using LitJson;
using Newtonsoft.Json;
using SuperSocket.ClientEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;


//로그인 관련 기능을 정의합니다.
public class UserLoginBackEnd 
{
    
    /// <summary>
    /// 비동기로 구글 로그인을 실행합니다.
    /// </summary>
    public async UniTask<bool> GoogleLoginAsync()
    {
        var utcs = new UniTaskCompletionSource<KeyValuePair<bool, string>>();

        TheBackend.ToolKit.GoogleLogin.Android.GoogleLogin(true, (isSuccess, errorMessage, token) =>
        {
            Logger.Log($"[GoogleLogin] isSuccess={isSuccess}, errorMessage={errorMessage}");
            utcs.TrySetResult(new KeyValuePair<bool, string>(isSuccess, token));
        });

        try
        {
            var res = await utcs.Task.Timeout(TimeSpan.FromSeconds(10));

            if (res.Key == false)
            {
                Logger.Log("구글 로그인 실패");
                return false;
            }

            Logger.Log("구글 로그인 성공");
            await UniTask.SwitchToMainThread();
            var bro = Backend.BMember.AuthorizeFederation(res.Value, FederationType.Google);

            if (!bro.IsSuccess())
            {
                Logger.Log($"페더레이션 로그인 실패: statusCode={bro.GetStatusCode()}, errorCode={bro.GetErrorCode()}, message={bro.GetMessage()}");
                return false;
            }
    
            Logger.Log("페더레이션 로그인 성공");
        }
        catch (Exception e)
        {
            Logger.LogError($"{e.Message} 구글 로그인 에러");
            return false;
        }
    
        return true;
    }

    

    /// <summary>
    /// 구글 로그아웃을 실행합니다.
    /// </summary>
    public async UniTask<bool> GoogleLogOutAsync()
    {
        var utcs = new UniTaskCompletionSource<bool>();
        
        TheBackend.ToolKit.GoogleLogin.Android.GoogleSignOut(true, (isSuccess, errorMessage) =>
        {
            Logger.Log($"[GoogleLogin] isSuccess={isSuccess}, errorMessage={errorMessage}");
            utcs.TrySetResult(isSuccess);
        });

        try
        {
            var res = await utcs.Task.Timeout(TimeSpan.FromSeconds(10));

            if (!res)
            {
                Logger.LogError("구글 로그아웃에 실패했습니다.");
                return false;
            }

            Logger.Log("구글 로그아웃 성공");
        }
        catch (Exception e)
        {
            Logger.LogError($"{e.Message} 구글 로그아웃 타임아웃");
            return false;
        }
        
        return true;
    }

    
    /// <summary>
    /// 랜덤으로 닉네임을 설정합니다.
    /// </summary>
    /// <returns></returns>
    public async UniTask<(bool isSuccess, string candidateName)> CreateRandomNicknameAsync()
    {
        int retryCount = 0;
    
        while (retryCount < 5)
        {
            int randomNumber = UnityEngine.Random.Range(1000, 100000);
            string candidateName = $"Guest-{randomNumber}";
            
            bool isSuccess = await SetNickNameAsync(candidateName);
            if (isSuccess)
            {
                return (true ,candidateName);
            }

            retryCount++;
        }

        return (false, null);
    }
    
    
    
    /// <summary>
    /// 회원탈퇴를 진행합니다.
    /// </summary>
    public async UniTask<bool> WithDrawAccount()
    {
        var utcs = AutoResetUniTaskCompletionSource<BackendReturnObject>.Create();
        Backend.BMember.WithdrawAccount(0,(callback) =>
        {
            utcs.TrySetResult(callback);
        });

        var bro = await utcs.Task;
        return bro.IsSuccess();
    }

    
    
    //로그인 만약 게스트 계정이 없으면 새로 만들고 로그인
    public async UniTask<BackendReturnObject> LogInAsync()
    {
        var utcs = AutoResetUniTaskCompletionSource<BackendReturnObject>.Create();
        Backend.BMember.GuestLogin("Guest Login",(callback) => {
            utcs.TrySetResult(callback);
        });
        return await utcs.Task;
    }

    
    /// <summary>
    /// 게스트 로그 아웃을 수행합니다.
    /// </summary>
    /// <returns></returns>
    public async UniTask<bool> LogOutAsync()
    {
        var utcs = AutoResetUniTaskCompletionSource<BackendReturnObject>.Create();
        Backend.BMember.Logout((callback) => {
            utcs.TrySetResult(callback);
        });
        
        var bro = await utcs.Task;
        return bro.IsSuccess();
    }


    //이전에 로그인한 계정이 있으면 그 토큰으로 로그인합니다. 없으면 실패합니다.
    public async UniTask<BackendReturnObject> SignInAsync()
    {
        var utcs = AutoResetUniTaskCompletionSource<BackendReturnObject>.Create();
        Backend.BMember.LoginWithTheBackendToken((callback) => {
            utcs.TrySetResult(callback);
        });
        return  await utcs.Task;
    }

    
    //닉네임 저장
    public async UniTask<bool> SetNickNameAsync(string nickName)
    {
        var utcs = AutoResetUniTaskCompletionSource<BackendReturnObject>.Create();

        Backend.BMember.UpdateNickname(nickName, callback =>
        {
            utcs.TrySetResult(callback);
        });
        
        BackendReturnObject bro = await utcs.Task;
        
        return bro.IsSuccess();
    }


    /// <summary>
    /// 닉네임 중복을 체크합니다.
    /// </summary>
    /// <param name="name"></param>
    /// <returns>TURE일시 중복 False일시 중복이 아닙니다</returns>

    public async UniTask<bool> CheckNicknameDuplicationAsync(string name)
    {
        var utcs = AutoResetUniTaskCompletionSource<BackendReturnObject>.Create();

        Backend.BMember.CheckNicknameDuplication(name, callback =>
        {
            utcs.TrySetResult(callback);
        });

        var bro = await utcs.Task;

        return bro.IsSuccess();
    }

    
}
