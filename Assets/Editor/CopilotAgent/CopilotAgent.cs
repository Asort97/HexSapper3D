using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor utility that lets the agent apply scene edits from a JSON file.
// Commands file: Assets/Copilot/commands.json
// Menu: Tools/Copilot Agent/Apply Commands Now, Toggle Auto Apply, Open Commands File, Create Sample File
namespace CopilotAgent
{
    [InitializeOnLoad]
    public static class CopilotAgent
    {
        private const string AutoApplyKey = "CopilotAgent.AutoApply";
        private static readonly string CommandsDir = "Assets/Copilot";
        private static readonly string CommandsPath = Path.Combine(CommandsDir, "commands.json");
        private static double _nextPoll;
        private static DateTime _lastWriteTime;

        static CopilotAgent()
        {
            EditorApplication.update += Update;
        }

        [MenuItem("Tools/Copilot Agent/Apply Commands Now")] 
        public static void ApplyNowMenu()
        {
            TryApplyCommands();
        }

        [MenuItem("Tools/Copilot Agent/Toggle Auto Apply")] 
        public static void ToggleAutoApply()
        {
            bool current = EditorPrefs.GetBool(AutoApplyKey, true);
            EditorPrefs.SetBool(AutoApplyKey, !current);
            Debug.Log($"[CopilotAgent] Auto Apply: {!current}");
        }

        [MenuItem("Tools/Copilot Agent/Open Commands File")] 
        public static void OpenCommandsFile()
        {
            EnsureCommandsFolder();
            if (!File.Exists(CommandsPath))
            {
                File.WriteAllText(CommandsPath, SampleJson);
                AssetDatabase.ImportAsset(CommandsPath);
            }
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(CommandsPath);
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }

        [MenuItem("Tools/Copilot Agent/Create Sample File")] 
        public static void CreateSampleFile()
        {
            EnsureCommandsFolder();
            var samplePath = Path.Combine(CommandsDir, "commands.json.sample");
            File.WriteAllText(samplePath, SampleJson);
            AssetDatabase.ImportAsset(samplePath);
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(samplePath);
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }

        private static void Update()
        {
            // poll at ~1 Hz to avoid overhead
            if (EditorApplication.timeSinceStartup < _nextPoll) return;
            _nextPoll = EditorApplication.timeSinceStartup + 1.0;

            if (!EditorPrefs.GetBool(AutoApplyKey, true)) return;
            TryApplyCommands(auto: true);
        }

        private static void TryApplyCommands(bool auto = false)
        {
            try
            {
                if (!File.Exists(CommandsPath)) return;

                var info = new FileInfo(CommandsPath);
                if (auto && info.LastWriteTime == _lastWriteTime) return;

                _lastWriteTime = info.LastWriteTime;
                string json = File.ReadAllText(CommandsPath);
                if (string.IsNullOrWhiteSpace(json)) return;

                object deserializedObj = null;
                try
                {
                    deserializedObj = MiniJson.Deserialize(json);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CopilotAgent] Exception during JSON deserialization: {ex}\nJSON:\n{json}");
                    return;
                }

                if (deserializedObj == null)
                {
                    Debug.LogError("[CopilotAgent] MiniJson.Deserialize returned null.\nJSON:\n" + json);
                    return;
                }

                var parsed = deserializedObj as Dictionary<string, object>;
                if (parsed == null)
                {
                    Debug.LogError($"[CopilotAgent] Deserialized object is not a Dictionary<string,object>. Type: {deserializedObj.GetType().Name}\nJSON:\n{json}");
                    return;
                }
                if (!parsed.TryGetValue("commands", out var cmdsObj) || !(cmdsObj is IList list))
                {
                    Debug.LogWarning("[CopilotAgent] No 'commands' array found.");
                    return;
                }

                Undo.IncrementCurrentGroup();
                int undoGroup = Undo.GetCurrentGroup();

                int applied = 0;
                foreach (var item in list)
                {
                    if (!(item is Dictionary<string, object> cmd)) continue;
                    if (!cmd.TryGetValue("type", out var typeObj))
                    {
                        Debug.LogWarning("[CopilotAgent] Command missing 'type'. Skipped.");
                        continue;
                    }
                    string type = typeObj?.ToString();
                    try
                    {
                        switch (type)
                        {
                            case "InstantiatePrefab":
                                applied += CmdInstantiatePrefab(cmd) ? 1 : 0;
                                break;
                            case "CreateEmpty":
                                applied += CmdCreateEmpty(cmd) ? 1 : 0;
                                break;
                            case "SetTransform":
                                applied += CmdSetTransform(cmd) ? 1 : 0;
                                break;
                            case "SetRectTransform":
                                applied += CmdSetRectTransform(cmd) ? 1 : 0;
                                break;
                            case "AddComponent":
                                applied += CmdAddComponent(cmd) ? 1 : 0;
                                break;
                            case "SetProperty":
                                applied += CmdSetProperty(cmd) ? 1 : 0;
                                break;
                            case "SetActive":
                                applied += CmdSetActive(cmd) ? 1 : 0;
                                break;
                            case "SetParent":
                                applied += CmdSetParent(cmd) ? 1 : 0;
                                break;
                            default:
                                Debug.LogWarning($"[CopilotAgent] Unknown command type: {type}");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[CopilotAgent] Error executing {type}: {ex}");
                    }
                }

                if (applied > 0)
                {
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                    Undo.CollapseUndoOperations(undoGroup);
                    AssetDatabase.SaveAssets();
                    TryExportHierarchySnapshot();
                    // move processed commands aside
                    var processedName = Path.Combine(CommandsDir, $"commands.processed.{DateTime.Now:yyyyMMdd_HHmmss}.json");
                    File.Move(CommandsPath, processedName);
                    AssetDatabase.Refresh();
                    Debug.Log($"[CopilotAgent] Applied {applied} command(s). Processed file moved to: {processedName}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CopilotAgent] Apply failed: {ex}");
            }
        }

        private static void EnsureCommandsFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Copilot"))
            {
                AssetDatabase.CreateFolder("Assets", "Copilot");
            }
        }

        private static bool CmdInstantiatePrefab(Dictionary<string, object> cmd)
        {
            string prefabPath = GetString(cmd, "prefabPath");
            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError("[CopilotAgent] InstantiatePrefab: 'prefabPath' is required.");
                return false;
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[CopilotAgent] InstantiatePrefab: Prefab not found at '{prefabPath}'.");
                return false;
            }
            string name = GetString(cmd, "name");
            string parentPath = GetString(cmd, "parent");
            var parent = string.IsNullOrEmpty(parentPath) ? null : ResolveByPath(parentPath);
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) return false;
            Undo.RegisterCreatedObjectUndo(instance, "Instantiate Prefab");
            if (!string.IsNullOrEmpty(name)) instance.name = name;
            if (parent != null)
            {
                Undo.SetTransformParent(instance.transform, parent.transform, "Reparent");
            }
            ApplyTransform(instance.transform, cmd);
            return true;
        }

        private static bool CmdCreateEmpty(Dictionary<string, object> cmd)
        {
            string name = GetString(cmd, "name");
            if (string.IsNullOrEmpty(name)) name = "GameObject";
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create Empty");
            string parentPath = GetString(cmd, "parent");
            var parent = string.IsNullOrEmpty(parentPath) ? null : ResolveByPath(parentPath);
            if (parent != null)
            {
                Undo.SetTransformParent(go.transform, parent.transform, "Reparent");
            }
            ApplyTransform(go.transform, cmd);
            return true;
        }

        private static bool CmdSetTransform(Dictionary<string, object> cmd)
        {
            var t = ResolveTargetTransform(cmd);
            if (t == null) return false;
            ApplyTransform(t, cmd);
            return true;
        }

        private static bool CmdSetRectTransform(Dictionary<string, object> cmd)
        {
            var t = ResolveTargetTransform(cmd) as RectTransform;
            if (t == null)
            {
                Debug.LogError("[CopilotAgent] SetRectTransform: target is not a RectTransform.");
                return false;
            }
            if (TryGetVector2(cmd, "anchoredPosition", out var ap)) t.anchoredPosition = ap;
            if (TryGetVector2(cmd, "sizeDelta", out var sd)) t.sizeDelta = sd;
            if (TryGetVector2(cmd, "anchorMin", out var amin)) t.anchorMin = amin;
            if (TryGetVector2(cmd, "anchorMax", out var amax)) t.anchorMax = amax;
            if (TryGetVector2(cmd, "pivot", out var piv)) t.pivot = piv;
            return true;
        }

        private static bool CmdAddComponent(Dictionary<string, object> cmd)
        {
            var go = ResolveTargetGameObject(cmd);
            if (go == null) return false;
            string component = GetString(cmd, "component");
            if (string.IsNullOrEmpty(component))
            {
                Debug.LogError("[CopilotAgent] AddComponent: 'component' is required.");
                return false;
            }
            var type = ResolveType(component);
            if (type == null || !typeof(Component).IsAssignableFrom(type))
            {
                Debug.LogError($"[CopilotAgent] AddComponent: type '{component}' not found or not a Component.");
                return false;
            }
            Undo.AddComponent(go, type);
            return true;
        }

        private static bool CmdSetActive(Dictionary<string, object> cmd)
        {
            var go = ResolveTargetGameObject(cmd);
            if (go == null) return false;
            bool active = true;
            if (cmd.TryGetValue("active", out var activeObj))
            {
                if (activeObj is bool b) active = b;
                else active = activeObj.ToString().ToLowerInvariant() == "true";
            }
            Undo.RecordObject(go, "Set Active");
            go.SetActive(active);
            return true;
        }

        private static bool CmdSetParent(Dictionary<string, object> cmd)
        {
            var go = ResolveTargetGameObject(cmd);
            if (go == null) return false;
            string parentPath = GetString(cmd, "parent");
            Transform newParent = null;
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parentGo = ResolveByPath(parentPath);
                if (parentGo == null)
                {
                    Debug.LogError($"[CopilotAgent] SetParent: parent not found: {parentPath}");
                    return false;
                }
                newParent = parentGo.transform;
            }
            Undo.SetTransformParent(go.transform, newParent, "Set Parent");
            // Reset local position for UI elements
            if (go.GetComponent<RectTransform>() != null && newParent != null)
            {
                var rt = go.GetComponent<RectTransform>();
                rt.anchoredPosition = Vector2.zero;
            }
            return true;
        }

        private static bool CmdSetProperty(Dictionary<string, object> cmd)
        {
            var go = ResolveTargetGameObject(cmd);
            if (go == null) return false;
            string component = GetString(cmd, "component");
            string property = GetString(cmd, "property");
            if (string.IsNullOrEmpty(component) || string.IsNullOrEmpty(property))
            {
                Debug.LogError("[CopilotAgent] SetProperty: 'component' and 'property' are required.");
                return false;
            }
            var type = ResolveType(component);
            if (type == null)
            {
                Debug.LogError($"[CopilotAgent] SetProperty: type '{component}' not found.");
                return false;
            }
            var comp = go.GetComponent(type);
            if (comp == null)
            {
                Debug.LogError($"[CopilotAgent] SetProperty: component '{component}' not found on '{go.name}'.");
                return false;
            }

            // Determine value
            object value = null;
            if (cmd.ContainsKey("value"))
            {
                value = cmd["value"]; // primitives/arrays
            }
            else if (cmd.ContainsKey("valuePath"))
            {
                string vpath = GetString(cmd, "valuePath");
                var target = ResolveByPath(vpath);
                if (target != null)
                {
                    string objectType = GetString(cmd, "objectType");
                    if (!string.IsNullOrEmpty(objectType))
                    {
                        var ot = ResolveType(objectType) ?? typeof(UnityEngine.Object);
                        if (typeof(Component).IsAssignableFrom(ot))
                        {
                            value = target.GetComponent(ot);
                        }
                        else if (ot == typeof(GameObject))
                        {
                            value = target;
                        }
                        else
                        {
                            value = target; // try assign GameObject to Object
                        }
                    }
                    else value = target;
                }
            }
            else if (cmd.ContainsKey("assetPath"))
            {
                string ap = GetString(cmd, "assetPath");
                string objectType = GetString(cmd, "objectType");
                var ot = ResolveType(objectType) ?? typeof(UnityEngine.Object);
                value = AssetDatabase.LoadAssetAtPath(ap, ot);
            }

            if (!TryAssignMember(comp, property, value))
            {
                Debug.LogError($"[CopilotAgent] SetProperty: failed to assign '{property}' on '{component}'.");
                return false;
            }
            EditorUtility.SetDirty(comp);
            return true;
        }

        private static void ApplyTransform(Transform t, Dictionary<string, object> cmd)
        {
            if (TryGetVector3(cmd, "position", out var p)) t.position = p;
            if (TryGetVector3(cmd, "localPosition", out var lp)) t.localPosition = lp;
            if (TryGetVector3(cmd, "rotation", out var r)) t.rotation = Quaternion.Euler(r);
            if (TryGetVector3(cmd, "localEuler", out var le)) t.localRotation = Quaternion.Euler(le);
            if (TryGetVector3(cmd, "scale", out var s)) t.localScale = s;
        }

        private static Transform ResolveTargetTransform(Dictionary<string, object> cmd)
        {
            var go = ResolveTargetGameObject(cmd);
            return go ? go.transform : null;
        }

        private static GameObject ResolveTargetGameObject(Dictionary<string, object> cmd)
        {
            string target = GetString(cmd, "target");
            if (string.IsNullOrEmpty(target))
            {
                Debug.LogError("[CopilotAgent] Command missing 'target'.");
                return null;
            }
            var go = ResolveByPath(target);
            if (go == null)
            {
                Debug.LogError($"[CopilotAgent] Target not found by path: {target}");
            }
            return go;
        }

        private static GameObject ResolveByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var parts = path.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return null;
            var scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != parts[0]) continue;
                var current = root.transform;
                bool ok = true;
                for (int i = 1; i < parts.Length; i++)
                {
                    var child = current.Find(parts[i]);
                    if (child == null) { ok = false; break; }
                    current = child;
                }
                if (ok) return current.gameObject;
            }
            // fallback: search any transform by name (first match)
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true));
            var tf = all.FirstOrDefault(t => t.gameObject.name == parts.Last());
            return tf ? tf.gameObject : null;
        }

        private static bool TryGetVector3(Dictionary<string, object> dict, string key, out Vector3 v)
        {
            v = default;
            if (!dict.TryGetValue(key, out var obj) || obj == null) return false;
            if (obj is IList list && list.Count >= 3)
            {
                v = new Vector3(ToFloat(list[0]), ToFloat(list[1]), ToFloat(list[2]));
                return true;
            }
            if (obj is Dictionary<string, object> d)
            {
                float x = d.TryGetValue("x", out var xv) ? ToFloat(xv) : 0f;
                float y = d.TryGetValue("y", out var yv) ? ToFloat(yv) : 0f;
                float z = d.TryGetValue("z", out var zv) ? ToFloat(zv) : 0f;
                v = new Vector3(x, y, z);
                return true;
            }
            return false;
        }

        private static bool TryGetVector2(Dictionary<string, object> dict, string key, out Vector2 v)
        {
            v = default;
            if (!dict.TryGetValue(key, out var obj) || obj == null) return false;
            if (obj is IList list && list.Count >= 2)
            {
                v = new Vector2(ToFloat(list[0]), ToFloat(list[1]));
                return true;
            }
            if (obj is Dictionary<string, object> d)
            {
                float x = d.TryGetValue("x", out var xv) ? ToFloat(xv) : 0f;
                float y = d.TryGetValue("y", out var yv) ? ToFloat(yv) : 0f;
                v = new Vector2(x, y);
                return true;
            }
            return false;
        }

        private static float ToFloat(object o)
        {
            if (o == null) return 0f;
            if (o is float f) return f;
            if (o is double d) return (float)d;
            if (o is long l) return l;
            if (o is int i) return i;
            float.TryParse(o.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v);
            return v;
        }

        private static string GetString(Dictionary<string, object> dict, string key)
        {
            return dict.TryGetValue(key, out var o) && o != null ? o.ToString() : null;
        }

        private static Type ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            // Try full name
            var t = Type.GetType(typeName);
            if (t != null) return t;
            // Try UnityEngine or UnityEditor shortcuts
            t = Type.GetType("UnityEngine." + typeName + ", UnityEngine");
            if (t != null) return t;
            t = Type.GetType("UnityEditor." + typeName + ", UnityEditor");
            if (t != null) return t;
            // Scan all assemblies
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    t = asm.GetType(typeName);
                    if (t != null) return t;
                    t = asm.GetTypes().FirstOrDefault(x => x.Name == typeName || x.FullName == typeName);
                    if (t != null) return t;
                }
                catch { /* ignore dynamic assemblies */ }
            }
            return null;
        }

        private static bool TryAssignMember(object target, string memberName, object value)
        {
            var type = target.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var prop = type.GetProperty(memberName, flags);
            if (prop != null && prop.CanWrite)
            {
                object v = ConvertTo(value, prop.PropertyType);
                Undo.RecordObject(target as UnityEngine.Object, "Set Property");
                prop.SetValue(target, v);
                return true;
            }
            var field = type.GetField(memberName, flags);
            if (field != null)
            {
                object v = ConvertTo(value, field.FieldType);
                Undo.RecordObject(target as UnityEngine.Object, "Set Field");
                field.SetValue(target, v);
                return true;
            }
            return false;
        }

        private static object ConvertTo(object value, Type targetType)
        {
            if (targetType == typeof(string)) return value?.ToString();
            if (value == null)
            {
                if (targetType.IsValueType) return Activator.CreateInstance(targetType);
                return null;
            }
            if (targetType.IsAssignableFrom(value.GetType())) return value;
            if (targetType == typeof(int)) return (int)ToFloat(value);
            if (targetType == typeof(float)) return ToFloat(value);
            if (targetType == typeof(bool)) return value is bool b ? b : (value.ToString().ToLowerInvariant() == "true");
            if (targetType == typeof(Vector3))
            {
                if (value is IList list && list.Count >= 3)
                    return new Vector3(ToFloat(list[0]), ToFloat(list[1]), ToFloat(list[2]));
            }
            if (targetType == typeof(Vector2))
            {
                if (value is IList list2 && list2.Count >= 2)
                    return new Vector2(ToFloat(list2[0]), ToFloat(list2[1]));
            }
            if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
            {
                // Try cast
                if (value is UnityEngine.Object uo && targetType.IsAssignableFrom(uo.GetType())) return uo;
            }
            return System.Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        private static void TryExportHierarchySnapshot()
        {
            // Optional: call HierarchySnapshot.ExportNow() if present
            var type = ResolveType("HierarchySnapshot");
            if (type == null) return;
            var method = type.GetMethod("ExportNow", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null) return;
            try { method.Invoke(null, null); }
            catch { /* ignore */ }
        }

        private static string SampleJson =>
            "{\n  \"commands\": [\n    {\n      \"type\": \"CreateEmpty\",\n      \"name\": \"CoinsManager\",\n      \"parent\": \"Canvas\"\n    },\n    {\n      \"type\": \"AddComponent\",\n      \"target\": \"Canvas/CoinsManager\",\n      \"component\": \"CoinsManager\"\n    },\n    {\n      \"type\": \"SetProperty\",\n      \"target\": \"Canvas/CoinsManager\",\n      \"component\": \"CoinsManager\",\n      \"property\": \"coinsText\",\n      \"valuePath\": \"Canvas/Gameplay/curr\",\n      \"objectType\": \"TMPro.TextMeshProUGUI\"\n    }\n  ]\n}";
    }
}
