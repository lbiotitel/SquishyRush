using System;
using System.Collections;
using System.Collections.Generic;
using BackEnd;
using UnityEngine;

public class GoogleLoginTest : MonoBehaviour
{
    private void Start()
    {
        Backend.Initialize();
        string googlehash = Backend.Utils.GetGoogleHash();
        Debug.Log("구글 해시 키 : " + googlehash);
    }


    public void StartGoogleLogin() {
        TheBackend.ToolKit.GoogleLogin.Android.GoogleLogin(true, GoogleLoginCallback);
    }
    
    private void GoogleLoginCallback(bool isSuccess, string errorMessage, string token) {
        if (isSuccess == false) {
            Debug.LogError(errorMessage);
            return;
        }
        
        Debug.Log("구글 토큰 : " + token);
        
        var bro = Backend.BMember.AuthorizeFederation(token, FederationType.Google);
        Debug.Log("페데레이션 로그인 결과 : " + bro);
    }
}
