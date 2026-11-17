using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreditUI : BaseUI
{

    [SerializeField] private AnimButton closeButton;


    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);
        BindEvents();
    }


    private void BindEvents()
    {
        closeButton.OnClicked += CloseUI;
    }

    private void OnDestroy()
    {
        closeButton.OnClicked -= CloseUI;
    }
}
