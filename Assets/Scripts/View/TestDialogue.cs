using System;
using TMPro;
using UnityEngine;

public class TestDialogue : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text lineText;

    public Dialogues dialogues;

    private DialogueNode _currentNode;
    private int _lineIndex;

    private void Start() {
        _currentNode = GetNode("start");
        _lineIndex = 0;
    }

    private DialogueNode GetNode(string nodeID){
        var node = Array.Find(dialogues.dialogueNodes, n => n.id == nodeID);

        return node;
    }

    private void ShowCurrentLine(){
        var line = _currentNode.lines[_lineIndex];
        nameText.text = line.characterName;
        lineText.text = line.sentence;
    }
}
