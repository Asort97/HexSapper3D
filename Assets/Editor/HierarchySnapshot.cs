#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HierarchySnapshot
{
    private const string AssetPath = "Assets/HierarchySnapshot.md";
    private const string AutoKey = "HierarchySnapshot.Auto";

    [MenuItem("Tools/Hierarchy Snapshot/Export Now")] 
    public static void ExportHierarchy()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("Hierarchy Snapshot", "Active scene is invalid.", "OK");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"# Hierarchy Snapshot — {scene.name}");
        sb.AppendLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        foreach (var root in scene.GetRootGameObjects().OrderBy(go => go.transform.GetSiblingIndex()))
        {
            AppendGameObject(sb, root.transform, 0);
        }

        File.WriteAllText(AssetPath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        // Не открываем проводник каждый раз, чтобы не мешать авто-режиму
        Debug.Log($"Hierarchy Snapshot saved to {AssetPath}");
    }

    [MenuItem("Tools/Hierarchy Snapshot/Toggle Auto Export")] 
    public static void ToggleAuto()
    {
        bool curr = EditorPrefs.GetBool(AutoKey, false);
        bool next = !curr;
        EditorPrefs.SetBool(AutoKey, next);
        EditorUtility.DisplayDialog("Hierarchy Snapshot", $"Auto Export: {(next ? "ON" : "OFF")}", "OK");
    }

    private static void AppendGameObject(StringBuilder sb, Transform t, int indent)
    {
        string pad = new string(' ', indent * 2);
        var go = t.gameObject;
        string active = go.activeSelf ? "active" : "inactive";
        string inHierarchy = go.activeInHierarchy ? "(enabled)" : "(disabled chain)";
        sb.AppendLine($"{pad}- {go.name} [{active}] {inHierarchy}");
        sb.AppendLine($"{pad}  layer: {LayerMask.LayerToName(go.layer)} | tag: {go.tag}");
        var p = t.position; var r = t.eulerAngles; var s = t.localScale;
        sb.AppendLine($"{pad}  pos: ({p.x:F2},{p.y:F2},{p.z:F2}) rot: ({r.x:F1},{r.y:F1},{r.z:F1}) scale: ({s.x:F2},{s.y:F2},{s.z:F2})");

        // Components
        var comps = go.GetComponents<Component>();
        foreach (var c in comps)
        {
            if (c == null)
            {
                sb.AppendLine($"{pad}  * Missing Component");
                continue;
            }
            string compName = c.GetType().Name;
            string enabledStr = "";
            if (c is Behaviour b)
            {
                enabledStr = b.enabled ? "[enabled]" : "[disabled]";
            }
            else if (c is Renderer rComp)
            {
                enabledStr = rComp.enabled ? "[enabled]" : "[disabled]";
            }
            sb.AppendLine($"{pad}  * {compName} {enabledStr}");
        }

        // Children
        for (int i = 0; i < t.childCount; i++)
        {
            AppendGameObject(sb, t.GetChild(i), indent + 1);
        }
    }

    private static bool AutoEnabled => EditorPrefs.GetBool(AutoKey, false);

    private static double _nextExportTime;
    private const double DebounceSeconds = 0.5;

    private static void DebouncedExport()
    {
        _nextExportTime = EditorApplication.timeSinceStartup + DebounceSeconds;
        EditorApplication.update -= TryExportTick;
        EditorApplication.update += TryExportTick;
    }

    private static void TryExportTick()
    {
        if (EditorApplication.timeSinceStartup >= _nextExportTime)
        {
            EditorApplication.update -= TryExportTick;
            if (AutoEnabled) ExportHierarchy();
        }
    }

    [InitializeOnLoadMethod]
    private static void HookEvents()
    {
        // Хуки на изменения и плей-мод
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnHierarchyChanged()
    {
        if (!AutoEnabled) return;
        DebouncedExport();
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (!AutoEnabled) return;
        if (change == PlayModeStateChange.EnteredPlayMode || change == PlayModeStateChange.EnteredEditMode)
        {
            DebouncedExport();
        }
    }
}
#endif
