using UnityEditor;
using UnityEditor.Build;
using System.Linq;

public static class StoreSwitcher
{
    const string BaseId = "com.TinyTeams.SquishyRush";
    const string OneStoreDefine = "ONESTORE";

    [MenuItem("Store/Google Play")]
    static void GooglePlay() => Apply(BaseId, false);

    [MenuItem("Store/ONE store")]
    static void OneStore() => Apply(BaseId + ".onestore", true);

    static void Apply(string id, bool oneStore)
    {
        var target = NamedBuildTarget.Android;
        PlayerSettings.SetApplicationIdentifier(target, id);

        var defines = PlayerSettings.GetScriptingDefineSymbols(target)
            .Split(';').Where(d => d != "" && d != OneStoreDefine).ToList();
        if (oneStore) defines.Add(OneStoreDefine);
        PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));

        UnityEngine.Debug.Log($"[Store] {id} / ONESTORE={oneStore}");
    }
}   