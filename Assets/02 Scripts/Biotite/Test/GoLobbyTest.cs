using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GoLobbyTest : MonoBehaviour
{


    public void GoLobby()
    {
        GlobalManagers.Scene.NewTransition()
            .Unload(nameof(EScene.InGame))
            .UnLoadResources(nameof(EScene.InGame))
            .Load(nameof(EScene.Lobby), true)
            .LoadResources(nameof(EScene.Lobby))
            .WithOverlay()
            .Perform().Forget();
    }
}
