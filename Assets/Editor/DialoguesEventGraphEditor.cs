// Assets/Editor/DialoguesEventGraphEditor.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class DialoguesEventGraphEditor : EditorWindow
{
    // Editor state
    private Dialogues dialoguesAsset;
    private DialogueNode selectedNode;
    private Vector2 canvasOffset;
    private Dictionary<DialogueNode, int> nodeWindowIds = new Dictionary<DialogueNode, int>();
    private int nextWindowId = 1000;
    private Vector2 inspectorScroll;

    // list of available event types to instantiate (extend this list)
    private static readonly Type[] availableEventTypes = new Type[]
    {
        typeof(SayEvent),
        typeof(ExitCharacterEvent)
        // add your other DialogueEvent subclasses here
    };

    [MenuItem("VN/Event Graph Editor")]
    public static void OpenWindow()
    {
        var w = GetWindow<DialoguesEventGraphEditor>();
        w.titleContent = new GUIContent("VN Event Graph");
        w.Show();
    }

    private void OnEnable()
    {
        nodeWindowIds.Clear();
    }

    private void OnGUI()
    {
        DrawToolbar();

        HandleCanvasInput();

        DrawCanvasBackground();

        if (dialoguesAsset == null)
        {
            DrawEmptyHint();
            return;
        }

        EnsureWindowIds();

        // draw connections first
        DrawAllConnections();

        // draw node windows
        BeginWindows();
        for (int i = 0; i < dialoguesAsset.dialogueNodes.Length; i++)
        {
            DialogueNode node = dialoguesAsset.dialogueNodes[i];
            int id = nodeWindowIds[node];
            Rect r = new Rect(node.position + canvasOffset, node.size);
            r = GUI.Window(id, r, winId => DrawNodeWindow(winId, node), node.id);
            node.position = r.position - canvasOffset;
            node.size = r.size;
        }
        EndWindows();

        // right inspector
        DrawInspectorPanel();
    }

    #region Toolbar / Canvas

    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        dialoguesAsset = (Dialogues)EditorGUILayout.ObjectField(dialoguesAsset, typeof(Dialogues), false, GUILayout.Width(250));

        if (GUILayout.Button("Load", EditorStyles.toolbarButton))
        {
            if (dialoguesAsset != null) Repaint();
            else EditorUtility.DisplayDialog("No asset", "Assign Dialogues asset first", "OK");
        }

        if (GUILayout.Button("Create Node", EditorStyles.toolbarButton))
        {
            if (dialoguesAsset == null) { EditorUtility.DisplayDialog("No asset", "Assign Dialogues asset first", "OK"); }
            else CreateNodeAt(Vector2.zero - canvasOffset + new Vector2(200, 200));
        }

        if (GUILayout.Button("Save", EditorStyles.toolbarButton))
        {
            SaveAsset();
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Fit", EditorStyles.toolbarButton))
        {
            canvasOffset = Vector2.zero;
        }

        GUILayout.EndHorizontal();
    }

    private void DrawCanvasBackground()
    {
        Rect canvasRect = new Rect(0, 20, position.width - 350, position.height - 20);
        GUI.BeginGroup(canvasRect);
        EditorGUI.DrawRect(new Rect(0, 0, canvasRect.width, canvasRect.height), new Color(0.12f, 0.12f, 0.12f));
        // optional grid
        DrawGrid(new Rect(0,0,canvasRect.width, canvasRect.height), 32, 0.03f);
        GUI.EndGroup();
    }

    private void DrawGrid(Rect rect, float spacing, float opacity)
    {
        Handles.BeginGUI();
        Color prev = Handles.color;
        Handles.color = new Color(1f, 1f, 1f, opacity);
        for (float x = rect.x; x < rect.x + rect.width; x += spacing)
            Handles.DrawLine(new Vector3(x, rect.y), new Vector3(x, rect.y + rect.height));
        for (float y = rect.y; y < rect.y + rect.height; y += spacing)
            Handles.DrawLine(new Vector3(rect.x, y), new Vector3(rect.x + rect.width, y));
        Handles.color = prev;
        Handles.EndGUI();
    }
    private void DrawEmptyHint()
    {
        Rect r = new Rect(0, 20, position.width - 350, position.height - 20);
        GUI.BeginGroup(r);
        EditorGUILayout.HelpBox("Assign a Dialogues asset in the toolbar to edit its nodes and events.", MessageType.Info);
        GUI.EndGroup();
    }

    private void HandleCanvasInput()
    {
        Event e = Event.current;
        Rect canvasRect = new Rect(0, 20, position.width - 350, position.height - 20);

        // pan with right mouse drag or middle
        if (e.type == EventType.MouseDrag && (e.button == 2 || e.button == 1))
        {
            canvasOffset += e.delta;
            e.Use();
            Repaint();
        }

        // context menu (create node)
        if (e.type == EventType.ContextClick && canvasRect.Contains(e.mousePosition))
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Create Node"), false, () => CreateNodeAt(e.mousePosition - canvasOffset));
            menu.ShowAsContext();
            e.Use();
        }
    }

    #endregion

    #region Nodes & Connections

    private void EnsureWindowIds()
    {
        if (dialoguesAsset == null || dialoguesAsset.dialogueNodes == null) return;
        foreach (var n in dialoguesAsset.dialogueNodes)
        {
            if (!nodeWindowIds.ContainsKey(n)) nodeWindowIds[n] = nextWindowId++;
        }

        // cleanup removed nodes
        var keys = nodeWindowIds.Keys.ToList();
        foreach (var k in keys)
            if (!dialoguesAsset.dialogueNodes.Contains(k))
                nodeWindowIds.Remove(k);
    }

    private void DrawNodeWindow(int winId, DialogueNode node)
    {
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("X", GUILayout.Width(22)))
        {
            if (EditorUtility.DisplayDialog("Delete node", $"Delete node '{node.id}'?", "Delete", "Cancel"))
            {
                DeleteNode(node);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                return;
            }
        }
        GUILayout.Label(node.id, EditorStyles.boldLabel);
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        GUILayout.Label($"Events: {(node.events != null ? node.events.Count : 0)}", EditorStyles.miniLabel);
        if (GUILayout.Button("Add Event"))
        {
            ShowAddEventMenu(node);
        }

        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Center"))
        {
            canvasOffset = -node.position + new Vector2(100, 100);
        }
        if (GUILayout.Button("Select"))
        {
            selectedNode = node;
            Repaint();
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        // make window draggable
        GUI.DragWindow(new Rect(0, 0, 10000, 24));
    }

    private void DrawAllConnections()
    {
        if (dialoguesAsset == null || dialoguesAsset.dialogueNodes == null) return;
        var nodes = dialoguesAsset.dialogueNodes;
        for (int i = 0; i < nodes.Length; i++)
        {
            var n = nodes[i];
            if (n.choices == null) continue;
            Rect fromRect = new Rect(n.position + canvasOffset, n.size);
            for (int ci = 0; ci < n.choices.Length; ci++)
            {
                var choice = n.choices[ci];
                if (string.IsNullOrEmpty(choice.nextId)) continue;
                var target = nodes.FirstOrDefault(x => x.id == choice.nextId);
                if (target == null) continue;
                Rect toRect = new Rect(target.position + canvasOffset, target.size);
                Vector2 start = new Vector2(fromRect.xMax, fromRect.y + 30 + ci * 20);
                Vector2 end = new Vector2(toRect.xMin, toRect.y + toRect.height * 0.5f);
                DrawBezier(start, end, Color.cyan);
            }
        }
    }
    private void DrawBezier(Vector2 start, Vector2 end, Color color)
    {
        Handles.BeginGUI();
        Vector2 startT = start + Vector2.right * 80;
        Vector2 endT = end + Vector2.left * 80;
        Handles.DrawBezier(start, end, startT, endT, color, null, 3f);
        Handles.EndGUI();
    }

    #endregion

    #region Node lifecycle

    private void CreateNodeAt(Vector2 pos)
    {
        if (dialoguesAsset == null) return;
        var list = (dialoguesAsset.dialogueNodes ?? new DialogueNode[0]).ToList();
        DialogueNode newNode = new DialogueNode()
        {
            id = GetUniqueNodeId("node"),
            events = new List<DialogueEvent>(),
            choices = new DialogueChoice[0],
            position = pos,
            size = new Vector2(300, 140)
        };
        list.Add(newNode);
        dialoguesAsset.dialogueNodes = list.ToArray();
        EditorUtility.SetDirty(dialoguesAsset);
        SaveAsset();
    }

    private string GetUniqueNodeId(string baseId)
    {
        int i = 1;
        string candidate = $"{baseId}_{i}";
        var ids = (dialoguesAsset.dialogueNodes ?? new DialogueNode[0]).Select(n => n.id).ToHashSet();
        while (ids.Contains(candidate))
        {
            i++;
            candidate = $"{baseId}_{i}";
        }
        return candidate;
    }

    private void DeleteNode(DialogueNode node)
    {
        if (dialoguesAsset == null) return;
        var list = (dialoguesAsset.dialogueNodes ?? new DialogueNode[0]).ToList();
        list.Remove(node);
        // clear references
        foreach (var n in list)
        {
            if (n.choices == null) continue;
            for (int i = 0; i < n.choices.Length; i++)
            {
                if (n.choices[i].nextId == node.id) n.choices[i].nextId = "";
            }
        }
        dialoguesAsset.dialogueNodes = list.ToArray();
        nodeWindowIds.Remove(node);
        EditorUtility.SetDirty(dialoguesAsset);
        SaveAsset();
    }

    #endregion

    #region Add / Remove / Reorder Events (SerializeReference handling)

    private void ShowAddEventMenu(DialogueNode node)
    {
        GenericMenu menu = new GenericMenu();
        for (int i = 0; i < availableEventTypes.Length; i++)
        {
            var t = availableEventTypes[i];
            menu.AddItem(new GUIContent(t.Name), false, () => AddEventToNode(node, t));
        }
        menu.ShowAsContext();
    }

    private void AddEventToNode(DialogueNode node, Type eventType)
    {
        if (dialoguesAsset == null) return;

        SerializedObject so = new SerializedObject(dialoguesAsset);
        // find node index
        var nodesProp = so.FindProperty("dialogueNodes");
        int nodeIndex = -1;
        for (int i = 0; i < nodesProp.arraySize; i++)
        {
            var nodeProp = nodesProp.GetArrayElementAtIndex(i);
            var idProp = nodeProp.FindPropertyRelative("id");
            if (idProp != null && idProp.stringValue == node.id) { nodeIndex = i; break; }
        }
        if (nodeIndex < 0) return;

        var nodePropRef = nodesProp.GetArrayElementAtIndex(nodeIndex);
        var eventsProp = nodePropRef.FindPropertyRelative("events");
        // ensure list exists
        if (eventsProp == null)
        {
            Debug.LogError("Node has no 'events' property (expected List<DialogueEvent> with SerializeReference).");
            return;
        }

        // insert a new element
        int insertIndex = eventsProp.arraySize;
        eventsProp.arraySize++;
        so.ApplyModifiedProperties();

        var elem = eventsProp.GetArrayElementAtIndex(insertIndex);
#if UNITY_2019_3_OR_NEWER
        // create instance and set managedReferenceValue
        var instance = Activator.CreateInstance(eventType);
        elem.managedReferenceValue = instance;
        so.ApplyModifiedProperties();
#else
        Debug.LogError("SerializeReference APIs require Unity 2019.3+.");
#endif
// update runtime copy
        EditorUtility.SetDirty(dialoguesAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void RemoveEventFromNode(DialogueNode node, int eventIndex)
    {
        if (dialoguesAsset == null) return;
        var so = new SerializedObject(dialoguesAsset);
        var nodesProp = so.FindProperty("dialogueNodes");
        int nodeIndex = -1;
        for (int i = 0; i < nodesProp.arraySize; i++)
        {
            var nodeProp = nodesProp.GetArrayElementAtIndex(i);
            var idProp = nodeProp.FindPropertyRelative("id");
            if (idProp != null && idProp.stringValue == node.id) { nodeIndex = i; break; }
        }
        if (nodeIndex < 0) return;

        var nodePropRef = nodesProp.GetArrayElementAtIndex(nodeIndex);
        var eventsProp = nodePropRef.FindPropertyRelative("events");
        if (eventsProp == null) return;

        // clear managed reference then delete
        var elem = eventsProp.GetArrayElementAtIndex(eventIndex);
#if UNITY_2019_3_OR_NEWER
        elem.managedReferenceValue = null;
        eventsProp.DeleteArrayElementAtIndex(eventIndex); // removes null entry
        eventsProp.DeleteArrayElementAtIndex(eventIndex); // actually remove element (Unity quirk)
        so.ApplyModifiedProperties();
#else
        Debug.LogError("SerializeReference APIs require Unity 2019.3+.");
#endif
        EditorUtility.SetDirty(dialoguesAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void MoveEvent(DialogueNode node, int indexFrom, int indexTo)
    {
        if (indexFrom == indexTo) return;
        if (dialoguesAsset == null) return;

        var so = new SerializedObject(dialoguesAsset);
        var nodesProp = so.FindProperty("dialogueNodes");
        int nodeIndex = -1;
        for (int i = 0; i < nodesProp.arraySize; i++)
        {
            var nodeProp = nodesProp.GetArrayElementAtIndex(i);
            var idProp = nodeProp.FindPropertyRelative("id");
            if (idProp != null && idProp.stringValue == node.id) { nodeIndex = i; break; }
        }
        if (nodeIndex < 0) return;

        var nodePropRef = nodesProp.GetArrayElementAtIndex(nodeIndex);
        var eventsProp = nodePropRef.FindPropertyRelative("events");
        if (eventsProp == null) return;

        eventsProp.MoveArrayElement(indexFrom, indexTo);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(dialoguesAsset);
    }

    #endregion

    #region Inspector - edit selected node + events

    private void DrawInspectorPanel()
    {
        Rect inspRect = new Rect(position.width - 350, 20, 350, position.height - 20);
        GUILayout.BeginArea(inspRect, EditorStyles.helpBox);

        inspectorScroll = GUILayout.BeginScrollView(inspectorScroll);

        GUILayout.Label("Inspector", EditorStyles.boldLabel);

        if (dialoguesAsset == null)
        {
            GUILayout.Label("Assign Dialogues asset above.");
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            return;
        }

        // nodes list
        GUILayout.Label("Nodes", EditorStyles.miniBoldLabel);
        if (dialoguesAsset.dialogueNodes != null)
        {
            foreach (var n in dialoguesAsset.dialogueNodes)
            {
                GUIStyle style = (n == selectedNode) ? EditorStyles.whiteLabel : EditorStyles.label;
                if (GUILayout.Button(n.id, style))
                {
                    selectedNode = n;
                    Repaint();
                }
            }
        }

        EditorGUILayout.Space();

        if (selectedNode != null)
        {
            GUILayout.Label($"Node: {selectedNode.id}", EditorStyles.boldLabel);

            // basic node fields
            EditorGUI.BeginChangeCheck();
            selectedNode.id = EditorGUILayout.TextField("ID", selectedNode.id);
            selectedNode.position = EditorGUILayout.Vector2Field("Position", selectedNode.position);
            selectedNode.size = EditorGUILayout.Vector2Field("Size", selectedNode.size);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(dialoguesAsset);
                SaveAsset();
            }

            EditorGUILayout.Space();
            GUILayout.Label("Events", EditorStyles.miniBoldLabel);

            // draw events list using serialized property for proper serializeReference rendering
            SerializedObject so = new SerializedObject(dialoguesAsset);
            var nodesProp = so.FindProperty("dialogueNodes");
            int nodeIndex = -1;
            for (int i = 0; i < nodesProp.arraySize; i++)
            {
                var nodeProp = nodesProp.GetArrayElementAtIndex(i);
                var idProp = nodeProp.FindPropertyRelative("id");
                if (idProp != null && idProp.stringValue == selectedNode.id) { nodeIndex = i; break; }
            }

            if (nodeIndex >= 0)
            {
                var nodePropRef = nodesProp.GetArrayElementAtIndex(nodeIndex);
                var eventsProp = nodePropRef.FindPropertyRelative("events");

                if (eventsProp == null)
                {
                    GUILayout.Label("Node has no events list (expected SerializeReference List<DialogueEvent>)", EditorStyles.helpBox);
                }
                else
                {
                    // iterate events and render each with controls
                    for (int ei = 0; ei < eventsProp.arraySize; ei++)
                    {
                        var eventProp = eventsProp.GetArrayElementAtIndex(ei);

                        GUILayout.BeginVertical(EditorStyles.helpBox);
                        GUILayout.BeginHorizontal();
                        // show type name
                        string typeName = eventProp.managedReferenceFullTypename;
                        if (!string.IsNullOrEmpty(typeName))
                        {
                            // managedReferenceFullTypename looks like "AssemblyName TypeName"
                            var displayTypeName = eventProp.managedReferenceFullTypename.Split(' ').Last();
                            GUILayout.Label(displayTypeName, EditorStyles.boldLabel);
                        }
                        else GUILayout.Label("null", EditorStyles.boldLabel);

                        if (GUILayout.Button("▲", GUILayout.Width(24)) && ei > 0)
                        {
                            MoveEvent(selectedNode, ei, ei - 1);
                            so.Update();
                            continue;
                        }
                        if (GUILayout.Button("▼", GUILayout.Width(24)) && ei < eventsProp.arraySize - 1)
                        {
                            MoveEvent(selectedNode, ei, ei + 1);
                            so.Update();
                            continue;
                        }
                        if (GUILayout.Button("X", GUILayout.Width(24)))
                        {
                            RemoveEventFromNode(selectedNode, ei);
                            so.Update();
                            continue;
                        }
                        GUILayout.EndHorizontal();

                        // actual event fields
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(eventProp, new GUIContent("Event"), true);
                        EditorGUI.indentLevel--;

                        GUILayout.EndVertical();
                    }
                }

                // add event dropdown
                if (GUILayout.Button("+ Add Event"))
                {
                    ShowAddEventMenu(selectedNode);
                    // reserialize to reflect new element
                    so.Update();
                }
                so.ApplyModifiedProperties();
            }
            else
            {
                GUILayout.Label("Selected node not found in asset!");
            }

            EditorGUILayout.Space();

            // choices editing (simple)
            GUILayout.Label("Choices", EditorStyles.miniBoldLabel);
            if (selectedNode.choices == null) selectedNode.choices = new DialogueChoice[0];
            for (int i = 0; i < selectedNode.choices.Length; i++)
            {
                GUILayout.BeginHorizontal();
                selectedNode.choices[i].choiceText = EditorGUILayout.TextField(selectedNode.choices[i].choiceText);
                selectedNode.choices[i].nextId = EditorGUILayout.TextField(selectedNode.choices[i].nextId);
                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    var tmp = selectedNode.choices.ToList();
                    tmp.RemoveAt(i);
                    selectedNode.choices = tmp.ToArray();
                    EditorUtility.SetDirty(dialoguesAsset);
                    SaveAsset();
                    break;
                }
                GUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Add Choice"))
            {
                var tmp = selectedNode.choices.ToList();
                tmp.Add(new DialogueChoice { choiceText = "New choice", nextId = "" });
                selectedNode.choices = tmp.ToArray();
                EditorUtility.SetDirty(dialoguesAsset);
                SaveAsset();
            }

            if (GUILayout.Button("Save Node"))
            {
                EditorUtility.SetDirty(dialoguesAsset);
                SaveAsset();
            }
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    #endregion

    #region Utilities

    private void SaveAsset()
    {
        if (dialoguesAsset == null) return;
        EditorUtility.SetDirty(dialoguesAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    #endregion
}