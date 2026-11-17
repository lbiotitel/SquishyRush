using System;

public class ConsoleCommandBase
{

    private string commandID;
    private string commandDescription;
    private string commandFormat;

    public string CommandID { get => commandID; }
    public string CommandDescription { get => commandDescription; }
    public string CommandFormat { get => commandFormat; }


    public ConsoleCommandBase(string commandID, string commandDescription, string commandFormat)
    {
        this.commandID = commandID;
        this.commandDescription = commandDescription;
        this.commandFormat = commandFormat;
    }
}

public class ConsoleCommand : ConsoleCommandBase
{
    private Action commandAction;

    public ConsoleCommand(string commandID, string commandDescription, string commandFormat, Action commandAction) : base(commandID, commandDescription, commandFormat)
    {
        this.commandAction = commandAction;
    }

    public void Execute()
    {
        commandAction?.Invoke();
    }
}

public class ConsoleCommand<T1> : ConsoleCommandBase
{
    private Action<T1> commandAction;

    public ConsoleCommand(string commandID, string commandDescription, string commandFormat, Action<T1> commandAction) : base(commandID, commandDescription, commandFormat)
    {
        this.commandAction = commandAction;
    }

    public void Execute(T1 arg)
    {
        commandAction?.Invoke(arg);
    }
}


public class ConsoleCommand<T1,T2> : ConsoleCommandBase
{
    private Action<T1,T2> commandAction;

    public ConsoleCommand(string commandID, string commandDescription, string commandFormat, Action<T1,T2> commandAction) : base(commandID, commandDescription, commandFormat)
    {
        this.commandAction = commandAction;
    }

    public void Execute(T1 arg1,T2 arg2)
    {
        commandAction?.Invoke(arg1, arg2);
    }
}

public class ConsoleCommand<T1,T2,T3> : ConsoleCommandBase
{
    private Action<T1,T2,T3> commandAction;

    public ConsoleCommand(string commandID, string commandDescription, string commandFormat, Action<T1, T2, T3> commandAction) : base(commandID, commandDescription, commandFormat)
    {
        this.commandAction = commandAction;
    }

    public void Execute(T1 arg1,T2 arg2, T3 arg3)
    {
        commandAction?.Invoke(arg1,arg2,arg3);
    }
}