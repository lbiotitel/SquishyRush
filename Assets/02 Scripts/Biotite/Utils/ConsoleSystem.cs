#if UNITY_EDITOR
using BackEnd;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class ConsoleSystem : MonoBehaviour
{
    private bool showConsole = false;
    string input = "";

    private List<string> inputHistory = new List<string>();
    private int historyIndex = -1;



    private static ConsoleCommand DELETE_GUEST_COMMAND;
    private static ConsoleCommand<int> SET_RP_POINT_COMMAND;
    private static ConsoleCommand<int> SET_GOLD_COMMAND;
    private static ConsoleCommand<int> SET_GEM_COMMAND;
    private static ConsoleCommand<int> SET_IG_GOLD_COMMAND;
    private static ConsoleCommand<int> CLEAR_STAGE_COMMAND;
    private static ConsoleCommand IG_CLEAR_ROUND_COMMAND;
    private static ConsoleCommand IG_CLEAR_STAGE_COMMAND;

    private CurrencyService currencyService;

    private List<ConsoleCommandBase> commandList = new List<ConsoleCommandBase>();

    private Vector2 scrollPosition;

    private void Start()
    {
       DELETE_GUEST_COMMAND = new ConsoleCommand("delete_guest", "delete guest", "delete guest",() =>
        {
            Backend.BMember.DeleteGuestInfo();
            Logger.Log("게스트 정보가 삭제되었습니다.");
        });

        SET_RP_POINT_COMMAND = new ConsoleCommand<int>("Set_RP", "Setting Rp", "Setting Rp", (rp) =>
        {
            //Managers.UserDataManager.UserData.UserRP = rp;
            //Managers.BackEnd.UserRakingData.UpdateRankingData(Managers.UserDataManager.UserData);
            Logger.Log($"RP를 {rp}로 설정했습니다.");
        });

        SET_GOLD_COMMAND = new ConsoleCommand<int>("Set_Gold", "Setting Gold", "Setting Gold", (gold) =>
        {
            GlobalManagers.Service.GetService<CurrencyService>().AddGold(gold);
            Logger.Log($"골드를 {gold}만큼 증가했습니다.");
        });

        SET_GEM_COMMAND = new ConsoleCommand<int>("Set_Gem", "Setting GEM", "Setting GEM", (gem) =>
        {
            GlobalManagers.Service.GetService<CurrencyService>().AddGem(gem);
            Logger.Log($"젬을 {gem}만큼 증가했습니다.");
        });

        SET_IG_GOLD_COMMAND = new ConsoleCommand<int>("Set_IGGold", "Setting IGGold", "Setting IGGold", (amount) =>
        {
            InGameController controller = FindObjectOfType<InGameController>();
            if (controller == null)
            {
                Logger.LogWarning("인게임 씬이 아닙니다");
                return;
            }

            RunContext ctx = controller.Context;

            if (amount >= 0)
            {
                ctx.AddGold(amount);
            }
            else if (!ctx.TrySpendGold(-amount))
            {
                Logger.LogWarning($"골드 부족: 보유 {ctx. IGCurrency}");
                return;
            }
            
            Logger.Log($"인게임 골드 {amount:+#;-#;0} -> 현재 {ctx.IGCurrency}");
        });

        CLEAR_STAGE_COMMAND = new ConsoleCommand<int>("Clear_Stage", "Clear Stage", "Clear Stage", (index) =>
        {
            EStageType stageType = (EStageType)index;
            ProgressService progressService = GlobalManagers.Service.GetService<ProgressService>();
            progressService.SetStageProgress(stageType, true);
            Logger.Log($"{stageType} 스테이지를 클리어하였습니다.");
        });

        IG_CLEAR_ROUND_COMMAND = new ConsoleCommand("IG_ClearRound", "InGame Clear Round", "InGame Clear Round", () =>
        {
            InGameController controller = FindObjectOfType<InGameController>();
            if (controller == null)
            {
                Logger.LogWarning("인게임 씬이 아닙니다");
                return;
            }

            new InGameCheat(controller).ClearRound();
        });

        IG_CLEAR_STAGE_COMMAND = new ConsoleCommand("IG_ClearStage", "InGame Clear Stage", "InGame Clear Stage", () =>
        {
            InGameController controller = FindObjectOfType<InGameController>();
            if (controller == null)
            {
                Logger.LogWarning("인게임 씬이 아닙니다");
                return;
            }

            new InGameCheat(controller).ClearStage();
        });

        commandList.Add(DELETE_GUEST_COMMAND);
        commandList.Add(SET_RP_POINT_COMMAND);
        commandList.Add(SET_GOLD_COMMAND);
        commandList.Add(SET_GEM_COMMAND);
        commandList.Add(SET_IG_GOLD_COMMAND);
        commandList.Add(CLEAR_STAGE_COMMAND);
        commandList.Add(IG_CLEAR_ROUND_COMMAND);
        commandList.Add(IG_CLEAR_STAGE_COMMAND);
    }


    private void NavigateHistory(int direction)
    {
        if (inputHistory.Count == 0) return;

        historyIndex += direction;

        if (historyIndex < 0)
        {
            historyIndex = -1;
            input = "";
            return;
        }

        if (historyIndex >= inputHistory.Count)
        {
            historyIndex = inputHistory.Count - 1;
        }

        input = inputHistory[inputHistory.Count - 1 - historyIndex];
    }

    public void OnGUI()
    {
        Event currentEvent = Event.current;
        if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.F1)
        {
            showConsole = !showConsole;
            if (showConsole) input = "";
        }

        if (!showConsole) return;

        if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Return)
        {
            OnReturn();
            currentEvent.Use();
        }

        if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.UpArrow)
        {
            NavigateHistory(1);
        }
        else if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.DownArrow)
        {
            NavigateHistory(-1);
        }


        float consoleHeight = 300f;
        float inputFieldHeight = 50f;
        float padding = 10f;

        GUI.backgroundColor = new Color(0, 0, 0, 0.85f);
        GUI.Box(new Rect(0, 0, Screen.width, consoleHeight), "");

        GUIStyle logStyle = new GUIStyle(GUI.skin.label);
        logStyle.fontSize = 20;
        logStyle.normal.textColor = Color.green;

        float scrollAreaHeight = consoleHeight - inputFieldHeight - (padding * 2);
        Rect viewport = new Rect(padding, padding, Screen.width - 20, scrollAreaHeight);
        Rect content = new Rect(0, 0, Screen.width - 40, inputHistory.Count * 25f);

        scrollPosition = GUI.BeginScrollView(viewport, scrollPosition, content);
        for (int i = 0; i < inputHistory.Count; i++)
        {
            Rect labelRect = new Rect(5, i * 25f, content.width, 25f);
            GUI.Label(labelRect, $"> {inputHistory[i]}", logStyle);
        }
        GUI.EndScrollView();

        GUIStyle inputStyle = new GUIStyle(GUI.skin.textField);
        inputStyle.fontSize = 24;
        inputStyle.alignment = TextAnchor.MiddleLeft;


        float inputY = consoleHeight - inputFieldHeight + 5f;
        GUI.SetNextControlName("ConsoleInput");
        input = GUI.TextField(new Rect(padding, inputY, Screen.width - (padding * 2), 40f), input, inputStyle);
        GUI.FocusControl("ConsoleInput");
        
    }

    public void OnReturn()
    {
        if (string.IsNullOrWhiteSpace(input)) return;

        HandleInput();

        if (inputHistory.Count == 0 || inputHistory[inputHistory.Count - 1] != input)
        {
            inputHistory.Add(input);
        }

        historyIndex = -1;
        scrollPosition.y = inputHistory.Count * 25f;
        input = "";
    }

    private void HandleInput()
    {
        string[] properties = input.Split(' ');
        if (properties.Length == 0) return;

        for (int i = 0; i < commandList.Count; i++)
        {
            if (properties[0].ToLower() == commandList[i].CommandID.ToLower())
            {
                if (commandList[i] is ConsoleCommand command)
                {
                    command.Execute();
                }
                else if (commandList[i] is ConsoleCommand<int> intCommand)
                {
                    intCommand.Execute(int.Parse(properties[1]));
                }
                else if (commandList[i] is ConsoleCommand<string> stringCommands)
                { 
                    stringCommands.Execute(properties[1]);
                }
                else if (commandList[i] is ConsoleCommand<string, int> stringCommand)
                {
                    if (properties.Length > 2) stringCommand.Execute(properties[1], int.Parse(properties[2]));
                }
                else if (commandList[i] is ConsoleCommand<int, int, int> int3Command)
                {
                    if (properties.Length > 3)
                    {
                        int.TryParse(properties[1], out int x);
                        int.TryParse(properties[2], out int y);
                        int.TryParse(properties[3], out int z);
                        int3Command.Execute(x, y, z);
                    }
                }
            }
        }
    }
}
#endif