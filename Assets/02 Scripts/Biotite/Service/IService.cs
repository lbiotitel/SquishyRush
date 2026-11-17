using System.Collections;
using System.Collections.Generic;
using UnityEngine;



//서비스 로직의 인터페이스 정의입니다.
public interface IService 
{
    public void Initialize();
    public void Release();
}
