using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class InitController : MonoBehaviour
{
    
    private void Start()
    {
        
        
        //일반 플레이 모드
        GlobalManagers.Scene.NewTransition()
            .Unload(nameof(EScene.ForInit))
            .Load(nameof(EScene.Title))
            .LoadResources(nameof(EResourceLabel.Title))
            .Perform().Forget();
    }
}
