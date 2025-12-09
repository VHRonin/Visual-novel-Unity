using System;
using TMPro;
using UnityEngine;

public class DialogueSystem : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text lineText;

    public Dialogues dialogues;

    private DialogueNode _currentNode;
    private int _lineIndex;

    private void Start() {
        _currentNode = GetNode("start");
        _lineIndex = 0;
        ShowCurrentLine();
    }

    private DialogueNode GetNode(string nodeID){
        var node = Array.Find(dialogues.dialogueNodes, n => n.id == nodeID);

        return node;
    }

    public void ShowCurrentLine(){
        if (_lineIndex >= _currentNode.lines.Length) return;
        var line = _currentNode.lines[_lineIndex];
        nameText.text = line.character.ToString();
        lineText.text = line.sentence;
        _lineIndex++;
    }
}
