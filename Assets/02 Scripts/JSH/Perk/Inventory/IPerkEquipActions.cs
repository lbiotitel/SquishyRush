using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPerkEquipActions
{
    //재능 추가 시도
    EPerkEquipResult TryEquipPerkAt(int inventoryIndex, Vector2 screenPos);
}
