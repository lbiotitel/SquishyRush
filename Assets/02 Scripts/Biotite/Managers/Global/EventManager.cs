using System;
using System.Collections.Generic;



// 제네릭 이벤트 버스 구현
public class EventManager : IManager
{
    public delegate void EventDelegate<T>(T e) where T : IEventData;

    private Dictionary<Type, Delegate> delegates = new Dictionary<Type, Delegate>();

    public bool Initialize()
    {
        return true;
    }

    public void AddEvent<T>(EventDelegate<T> del) where T : IEventData
    {
        Type type = typeof(T);
        if (delegates.ContainsKey(type))
        {
            delegates[type] = Delegate.Combine(delegates[type], del);
        }
        else
        {
            delegates[type] = del;
        }
    }

    public void RemoveEvent<T>(EventDelegate<T> del) where T : IEventData
    {
        Type type = typeof(T);
        if (delegates.ContainsKey(type))
        {
            var currentDel = Delegate.Remove(delegates[type], del);
            if (currentDel == null) delegates.Remove(type);
            else delegates[type] = currentDel;
        }
    }

    public void NotifyEvent<T>(T e) where T : IEventData
    {
        Type type = typeof(T);
        if (delegates.TryGetValue(type, out Delegate del))
        {
            (del as EventDelegate<T>)?.Invoke(e);
        }
    }

    public void Release()
    {
        delegates.Clear();
    }
}