using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueNode
{
    public string id;
    public DialogueLine[] lines;
    public DialogueChoice[] choices;
    [SerializeReference]
    public List<DialogueEvent> events;

    public Vector2 position;
    public Vector2 size = new Vector2(240, 150);
}
