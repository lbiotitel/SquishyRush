#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// mallang_ui_borders.json 이 있는 폴더(및 하위 폴더)의 PNG를
/// UI 스프라이트로 임포트하고 9-slice 보더를 자동 적용한다.
/// json을 수정하면 해당 폴더의 스프라이트를 자동으로 다시 임포트한다.
/// </summary>
public class MallangUISpriteImporter : AssetPostprocessor
{
    private const string BorderFileName = "mallang_ui_borders.json";

    [Serializable] private class BorderItem { public string name; public int[] border; }
    [Serializable] private class BorderRoot { public BorderItem[] items; }

    // json 경로 -> (마지막 수정 시각, 보더 표)
    private static readonly Dictionary<string, (DateTime time, Dictionary<string, Vector4> map)> _cache = new();

    private void OnPreprocessTexture()
    {
        if (!assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;

        string borderFile = FindBorderFile(Path.GetDirectoryName(assetPath));
        if (borderFile == null) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);

        var borders = LoadBorders(borderFile);
        string spriteName = Path.GetFileNameWithoutExtension(assetPath);
        if (borders.TryGetValue(spriteName, out Vector4 b))
            importer.spriteBorder = b; // (L, B, R, T)
    }

    // json이 바뀌면 그 폴더 아래 PNG를 다시 임포트
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        foreach (string path in imported)
        {
            if (!path.EndsWith(BorderFileName, StringComparison.OrdinalIgnoreCase)) continue;

            _cache.Remove(path);
            string folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(folder)) continue;

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);

            Debug.Log($"[MallangUI] {path} 변경 감지 → 스프라이트 보더 다시 적용");
        }
    }

    private static string FindBorderFile(string dir)
    {
        while (!string.IsNullOrEmpty(dir))
        {
            string candidate = Path.Combine(dir, BorderFileName).Replace('\\', '/');
            if (File.Exists(candidate)) return candidate;
            if (dir.Replace('\\', '/') == "Assets") break;
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    private static Dictionary<string, Vector4> LoadBorders(string file)
    {
        DateTime time = File.GetLastWriteTimeUtc(file);
        if (_cache.TryGetValue(file, out var cached) && cached.time == time) return cached.map;

        var map = new Dictionary<string, Vector4>();
        try
        {
            var root = JsonUtility.FromJson<BorderRoot>(File.ReadAllText(file));
            if (root?.items != null)
            {
                foreach (var item in root.items)
                {
                    if (item.border == null || item.border.Length != 4) continue;
                    map[item.name] = new Vector4(item.border[0], item.border[1], item.border[2], item.border[3]);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[MallangUI] {file} 읽기 실패 (JSON 형식 확인): {e.Message}");
        }

        _cache[file] = (time, map);
        return map;
    }

    [MenuItem("Tools/Mallang UI/Reimport UI Sprites")]
    private static void ReimportAll()
    {
        _cache.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (FindBorderFile(Path.GetDirectoryName(path)) != null)
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }
}
#endif