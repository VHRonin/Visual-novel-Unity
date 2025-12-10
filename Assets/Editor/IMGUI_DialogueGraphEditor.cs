// Assets/Editor/IMGUI_DialogueGraphEditor.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// IMPORTANT: assumes your runtime classes are named exactly as you provided:
// Dialogues (ScriptableObject) -> public DialogueNode[] dialogueNodes;
// DialogueNode: public string id; public DialogueLine[] lines; public DialogueChoice[] choices; public Vector2 position; public Vector2 size;
// DialogueLine: public Characters characterName; [TextArea] public string sentence; public Emotion emotion;
// DialogueChoice: public string choiceText; public string nextId;

public class IMGUI_DialogueGraphEditor : EditorWindow
{
    private Dialogues dialoguesAsset;
    private Vector2 canvasDrag;
    private Vector2 canvasOffset;
    private float canvasZoom = 1f; // unused for now, kept for extension
    private Vector2 inspectorScroll;

    // Selection and interaction
    private DialogueNode selectedNode;
    private DialogueNode hoveredNode;
    private DialogueNode dragNode; // node currently being dragged via window system

    // Connection dragging
    private bool isDraggingConnection = false;
    private DialogueNode connectionFromNode = null;
    private int connectionFromChoiceIndex = -1;
    private Vector2 currentMousePosition;

    // Node GUI state (unique window ids)
    private Dictionary<DialogueNode, int> nodeWindowIds = new Dictionary<DialogueNode, int>();
    private int nextWindowId = 1000;

    // Layout
    private const float toolbarHeight = 22f;
    private const float inspectorWidth = 360f;
    private Rect canvasRect => new Rect(0, toolbarHeight, position.width - inspectorWidth, position.height - toolbarHeight);

    // Grid
    private Texture2D gridTexSmall;
    private Texture2D gridTexLarge;

    [MenuItem("VN/IMGUI Dialogue Graph")]
    public static void OpenWindow()
    {
        var w = GetWindow<IMGUI_DialogueGraphEditor>();
        w.titleContent = new GUIContent("Dialogue Graph (IMGUI)");
        w.Show();
    }

    private void OnEnable()
    {
        CreateGridTextures();
    }

    private void OnDisable()
    {
        nodeWindowIds.Clear();
    }

    private void OnGUI()
    {
        DrawToolbar();

        // handle events first for pan/zoom
        HandleCanvasEvents();

        // draw canvas background + grid
        DrawCanvasBackground();

        if (dialoguesAsset != null)
        {
            // draw connections first (below nodes)
            DrawAllConnections();

            // draw nodes as windows
            if (dialoguesAsset.dialogueNodes != null)
            {
                EnsureWindowIds();
                BeginWindows();
                for (int i = 0; i < dialoguesAsset.dialogueNodes.Length; i++)
                {
                    var node = dialoguesAsset.dialogueNodes[i];
                    var winId = nodeWindowIds[node];
                    Rect nodeRect = new Rect(node.position + canvasOffset, node.size);
                    nodeRect = GUI.Window(winId, nodeRect, id => DrawNodeWindow(id, node), node.id);
                    // update node position from window (store without canvasOffset)
                    node.position = nodeRect.position - canvasOffset;
                    node.size = nodeRect.size;
                }
                EndWindows();
            }
        }
        else
        {
            // hint
            GUI.BeginGroup(canvasRect);
            var hintRect = new Rect(10, 10, canvasRect.width - 20, 30);
            GUI.Label(hintRect, "Assign a Dialogues asset in the toolbar to start editing.", EditorStyles.helpBox);
            GUI.EndGroup();
        }

        // draw dragging connection on top
        if (isDraggingConnection && connectionFromNode != null)
        {
            DrawBezierFromNodeChoiceToPos(connectionFromNode, connectionFromChoiceIndex, currentMousePosition);
            Repaint();
        }

        // inspector on right
        DrawInspector();

        // process mouse up for finishing connection
        ProcessConnectionDrop();
    }
    #region Toolbar
    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Space(4);

        var newAsset = (Dialogues)EditorGUILayout.ObjectField(dialoguesAsset, typeof(Dialogues), false, GUILayout.Width(260));
        if (newAsset != dialoguesAsset)
        {
            dialoguesAsset = newAsset;
            // clear window ids to reassign
            nodeWindowIds.Clear();
        }

        if (GUILayout.Button("Load", EditorStyles.toolbarButton))
        {
            if (dialoguesAsset != null)
                Repaint();
            else
                EditorUtility.DisplayDialog("No Dialogues", "Assign a Dialogues asset first.", "OK");
        }

        if (GUILayout.Button("Create Node", EditorStyles.toolbarButton))
        {
            CreateNodeAt(new Vector2(200, 200) - canvasOffset);
        }

        if (GUILayout.Button("Save", EditorStyles.toolbarButton))
        {
            SaveAsset();
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Center", EditorStyles.toolbarButton))
        {
            canvasOffset = Vector2.zero;
        }

        GUILayout.EndHorizontal();
    }
    #endregion

    #region Canvas & Grid
    private void CreateGridTextures()
    {
        gridTexSmall = MakeTex(64, 64, new Color(0.9f, 0.9f, 0.9f, 0.05f));
        gridTexLarge = MakeTex(512, 512, new Color(0.9f, 0.9f, 0.9f, 0.03f));
    }

    private void DrawCanvasBackground()
    {
        // canvas area
        GUI.BeginGroup(canvasRect);
        Rect local = new Rect(0, 0, canvasRect.width, canvasRect.height);

        // background
        EditorGUI.DrawRect(local, new Color(0.13f, 0.13f, 0.13f));

        // draw grid with offset
        DrawGrid(local, 32, 0.08f, gridTexSmall);
        DrawGrid(local, 256, 0.03f, gridTexLarge);

        GUI.EndGroup();
    }

    private void DrawGrid(Rect area, float spacing, float alpha, Texture2D tex)
    {
        // tile the texture across canvas taking into account canvasOffset
        float xOffset = canvasOffset.x % spacing;
        float yOffset = canvasOffset.y % spacing;

        Rect r = new Rect(area.x + xOffset, area.y + yOffset, area.width, area.height);
        GUI.DrawTextureWithTexCoords(new Rect(area.x, area.y, area.width, area.height), tex, new Rect(0,0, area.width/tex.width, area.height/tex.height));
    }

    private Texture2D MakeTex(int w, int h, Color col)
    {
        Texture2D tex = new Texture2D(w, h);
        Color[] pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        tex.SetPixels(pix);
        tex.Apply();
        tex.hideFlags = HideFlags.DontSave;
        return tex;
    }
    #endregion

    #region Nodes
    private void EnsureWindowIds()
    {
        if (dialoguesAsset == null || dialoguesAsset.dialogueNodes == null) return;
        foreach (var n in dialoguesAsset.dialogueNodes)
        {
            if (!nodeWindowIds.ContainsKey(n))
            {
                nodeWindowIds[n] = nextWindowId++;
            }
        }
    }

    private void DrawNodeWindow(int id, DialogueNode node)
    {
        // header area clickable will be handled by default window dragging if we call DragWindow
        GUIStyle labelStyle = EditorStyles.boldLabel;
        GUILayout.BeginVertical();

        // show small header controls
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("X", GUILayout.Width(20)))
        {
            if (EditorUtility.DisplayDialog("Delete node", $"Delete node '{node.id}'?", "Delete", "Cancel"))
            {
                DeleteNode(node);
                return; // window no longer exists
            }
        }

        GUILayout.Label(node.id, labelStyle);
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        // show summary: number of lines and choices
        GUILayout.Label($"Lines: { (node.lines?.Length ?? 0) }   Choices: { (node.choices?.Length ?? 0) }", EditorStyles.miniLabel);

        GUILayout.Space(6);
        // draw output choice ports as tiny buttons on the right side (we will overlay afterwards)
        // We cannot draw overlay buttons easily inside Window content, so just show list of choice texts
        if (node.choices != null && node.choices.Length > 0)
        {
            for (int i = 0; i < node.choices.Length; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"[{i}] {node.choices[i].choiceText}", EditorStyles.label);
                GUILayout.EndHorizontal();
            }
        }

        GUILayout.EndVertical();

        // Handle input for starting a connection by clicking on the small area to the right of the window
        // We detect clicks at global mouse pos: nodeRect = Get current window rectangle via GUILayoutUtility?
        // But inside GUI.Window, we can get the window rect using GUILayoutUtility.GetLastRect() is unreliable.
        // Instead, use position from window id mapping in main OnGUI after windows drawn (we compute rects there).
        // To allow dragging window, call DragWindow on header region:
        GUI.DragWindow(new Rect(0, 0, 10000, 24));
    }

    private void CreateNodeAt(Vector2 localPos)
    {
        if (dialoguesAsset == null)
        {
            EditorUtility.DisplayDialog("No Dialogues", "Assign a Dialogues asset first.", "OK");
            return;
        }

        var n = new DialogueNode
        {
            id = GetUniqueNodeID("node"),
            lines = new DialogueLine[0],
            choices = new DialogueChoice[0],
            position = localPos,
            size = new Vector2(240, 120)
        };

        var list = (dialoguesAsset.dialogueNodes ?? new DialogueNode[0]).ToList();
        list.Add(n);
        dialoguesAsset.dialogueNodes = list.ToArray();
        EditorUtility.SetDirty(dialoguesAsset);
        SaveAsset();
    }

    private string GetUniqueNodeID(string baseId)
    {
        int i = 1;
        string candidate = $"{baseId}_{i}";
        var ids = (dialoguesAsset.dialogueNodes ?? new DialogueNode[0]).Select(x => x.id).ToHashSet();
        while (ids.Contains(candidate))
        {
            i++;
            candidate = $"{baseId}_{i}";
        }
        return candidate;
    }

    private void DeleteNode(DialogueNode node)
    {
        if (dialoguesAsset == null || dialoguesAsset.dialogueNodes == null) return;
        var list = dialoguesAsset.dialogueNodes.ToList();
        list.Remove(node);
        // clear references in other nodes
        foreach (var n in list)
        {
            if (n.choices == null) continue;
            foreach (var c in n.choices)
            {
                if (c.nextId == node.id) c.nextId = "";
            }
        }
        dialoguesAsset.dialogueNodes = list.ToArray();
        nodeWindowIds.Remove(node);
        EditorUtility.SetDirty(dialoguesAsset);
        SaveAsset();
    }
    #endregion

    #region Connections
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
    private void DrawBezierFromNodeChoiceToPos(DialogueNode fromNode, int choiceIndex, Vector2 toPos)
    {
        if (fromNode == null) return;
        Rect fromRect = new Rect(fromNode.position + canvasOffset, fromNode.size);
        Vector2 start = new Vector2(fromRect.xMax, fromRect.y + 30 + choiceIndex * 20);
        DrawBezier(start, toPos, Color.yellow);
    }

    private void DrawBezier(Vector2 start, Vector2 end, Color color)
    {
        Vector2 startTang = start + Vector2.right * 100f;
        Vector2 endTang = end + Vector2.left * 100f;
        Handles.BeginGUI();
        Handles.DrawBezier(start, end, startTang, endTang, color, null, 3f);
        Handles.EndGUI();
    }
    #endregion

    #region Interaction
    private void HandleCanvasEvents()
    {
        Event e = Event.current;
        currentMousePosition = e.mousePosition;

        // only consider events inside canvas
        if (!canvasRect.Contains(e.mousePosition) && e.type != EventType.ScrollWheel) return;

        // pan with middle mouse or right mouse drag
        if (e.type == EventType.MouseDrag && (e.button == 2 || e.button == 1))
        {
            canvasOffset += e.delta;
            e.Use();
            Repaint();
        }

        // start connection: detect clicks on node right output area
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            // check if clicked near any node's output port
            if (dialoguesAsset?.dialogueNodes != null)
            {
                foreach (var node in dialoguesAsset.dialogueNodes)
                {
                    Rect nodeRect = new Rect(node.position + canvasOffset, node.size);
                    // check each choice output position
                    if (node.choices != null)
                    {
                        for (int i = 0; i < node.choices.Length; i++)
                        {
                            Rect outHandle = new Rect(nodeRect.xMax - 12, nodeRect.y + 30 + i * 20 - 6, 12, 12);
                            if (outHandle.Contains(e.mousePosition))
                            {
                                // start dragging connection
                                isDraggingConnection = true;
                                connectionFromNode = node;
                                connectionFromChoiceIndex = i;
                                e.Use();
                                return;
                            }
                        }
                    }

                    // input handle (left) - if clicked without dragging, we could open selection or something
                    Rect inHandle = new Rect(nodeRect.xMin, nodeRect.y + nodeRect.height * 0.5f - 6, 12, 12);
                    if (inHandle.Contains(e.mousePosition))
                    {
                        // if we are dragging connection, drop here
                        if (isDraggingConnection && connectionFromNode != null)
                        {
                            // set connection
                            if (connectionFromNode != node) // avoid self-link
                            {
                                connectionFromNode.choices[connectionFromChoiceIndex].nextId = node.id;
                                EditorUtility.SetDirty(dialoguesAsset);
                                SaveAsset();
                                isDraggingConnection = false;
                                connectionFromNode = null;
                                connectionFromChoiceIndex = -1;
                                e.Use();
                                return;
                            }
                        }
                    }
                }
            }
        }

        // mouse release cancels connection creation if not dropped onto input
        if (e.type == EventType.MouseUp && e.button == 0)
        {
            if (isDraggingConnection)
            {
                isDraggingConnection = false;
                connectionFromNode = null;
                connectionFromChoiceIndex = -1;
                e.Use();
            }
        }
    }

    private void ProcessConnectionDrop()
    {
        // called each frame after drawing: check if mouse was released on a node input
        // We handled immediate drop in HandleCanvasEvents on MouseDown above. But also check mouse up position:
        Event e = Event.current;
        if (e.type == EventType.MouseUp && e.button == 0 && isDraggingConnection == false)
        {
            // nothing for now
        }
    }
    #endregion

    #region Inspector
private void DrawInspector()
{
    Rect inspRect = new Rect(position.width - inspectorWidth, toolbarHeight,
                             inspectorWidth, position.height - toolbarHeight);

    GUILayout.BeginArea(inspRect, EditorStyles.helpBox);
    {
        // 👇 ScrollView НАХОДИТСЯ СРАЗУ ПОСЛЕ BeginArea (НО НЕ ВНЕШЕ)
        inspectorScroll = GUILayout.BeginScrollView(inspectorScroll);

        GUILayout.Label("Inspector", EditorStyles.boldLabel);

        if (dialoguesAsset == null)
        {
            GUILayout.Label("Assign a Dialogues asset in toolbar.");
            if (GUILayout.Button("Create new Dialogues asset"))
            {
                var asset = CreateInstance<Dialogues>();
                AssetDatabase.CreateAsset(asset, "Assets/NewDialogues.asset");
                AssetDatabase.SaveAssets();
                dialoguesAsset = asset;
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = asset;
            }

            GUILayout.EndScrollView();  // ВАЖНО
            GUILayout.EndArea();        // ВАЖНО
            return;
        }

        GUILayout.Label("Nodes", EditorStyles.miniBoldLabel);

        if (dialoguesAsset.dialogueNodes != null)
        {
            foreach (var n in dialoguesAsset.dialogueNodes)
            {
                if (GUILayout.Button(n.id,
                    (selectedNode == n ? EditorStyles.whiteLabel : EditorStyles.label)))
                {
                    selectedNode = n;
                    Repaint();
                }
            }
        }

        EditorGUILayout.Space();

        if (selectedNode != null)
        {
            GUILayout.Label($"Editing Node: {selectedNode.id}", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            selectedNode.id = EditorGUILayout.TextField("ID", selectedNode.id);
            selectedNode.position = EditorGUILayout.Vector2Field("Position", selectedNode.position);
            selectedNode.size = EditorGUILayout.Vector2Field("Size", selectedNode.size);

            EditorGUILayout.Space();
            GUILayout.Label("Lines", EditorStyles.miniBoldLabel);

            if (selectedNode.lines == null)
                selectedNode.lines = new DialogueLine[0];

            for (int i = 0; i < selectedNode.lines.Length; i++)
            {
                var line = selectedNode.lines[i];
                GUILayout.BeginVertical(EditorStyles.helpBox);

                try
                {
                    line.characterName = (Characters)EditorGUILayout.EnumPopup("Character", line.characterName);
                }
                catch { GUILayout.Label("(Character enum missing)"); }

                line.sentence = EditorGUILayout.TextArea(line.sentence, GUILayout.Height(60));
                line.emotion = (Emotion)EditorGUILayout.EnumPopup("Emotion", line.emotion);

                if (GUILayout.Button("Remove Line"))
                {
                    var tmp = selectedNode.lines.ToList();
                    tmp.RemoveAt(i);
                    selectedNode.lines = tmp.ToArray();
                }
                GUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Line"))
            {
                var tmp = selectedNode.lines.ToList();
                tmp.Add(new DialogueLine { sentence = "New line", emotion = Emotion.Neutral });
                selectedNode.lines = tmp.ToArray();
            }

            EditorGUILayout.Space();
            GUILayout.Label("Choices", EditorStyles.miniBoldLabel);

            if (selectedNode.choices == null)
                selectedNode.choices = new DialogueChoice[0];

            for (int i = 0; i < selectedNode.choices.Length; i++)
            {
                var c = selectedNode.choices[i];
                GUILayout.BeginHorizontal();
                c.choiceText = EditorGUILayout.TextField(c.choiceText);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    var tmp = selectedNode.choices.ToList();
                    tmp.RemoveAt(i);
                    selectedNode.choices = tmp.ToArray();
                    EditorUtility.SetDirty(dialoguesAsset);
                    SaveAsset();
                    GUILayout.EndHorizontal();
                    GUILayout.EndScrollView();
                    GUILayout.EndArea();
                    return;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("→ NextId", GUILayout.Width(50));
                c.nextId = EditorGUILayout.TextField(c.nextId);

                if (GUILayout.Button("Pick", GUILayout.Width(40)))
                {
                    GenericMenu menu = new GenericMenu();
                    foreach (var n in dialoguesAsset.dialogueNodes)
                        menu.AddItem(new GUIContent(n.id), false, () =>
                        {
                            c.nextId = n.id;
                            EditorUtility.SetDirty(dialoguesAsset);
                            SaveAsset();
                        });

                    menu.ShowAsContext();
                }
                GUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Choice"))
            {
                var tmp = selectedNode.choices.ToList();
                tmp.Add(new DialogueChoice { choiceText = "New choice" });
                selectedNode.choices = tmp.ToArray();
            }

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(dialoguesAsset);

            EditorGUILayout.Space();

            if (GUILayout.Button("Center View On Node"))
                canvasOffset = -selectedNode.position + new Vector2(100, 100);

            if (GUILayout.Button("Delete Node"))
            {
                if (EditorUtility.DisplayDialog("Delete node", $"Delete node '{selectedNode.id}'?", "Delete", "Cancel"))
                {
                    DeleteNode(selectedNode);
                    selectedNode = null;
                }
            }
        }

        if (GUILayout.Button("Save Asset"))
            SaveAsset();

        GUILayout.EndScrollView();  // ← ВЫХОД 1
    }
    GUILayout.EndArea();            // ← ВЫХОД 2
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