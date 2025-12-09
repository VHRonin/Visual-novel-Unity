using System;
using System.Collections;
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

    private void ShowCurrentLine(){
        if (_lineIndex >= _currentNode.lines.Length) return;
        var line = _currentNode.lines[_lineIndex];
        nameText.text = line.characterName.ToString();
        lineText.text = line.sentence;
        StopAllCoroutines();
        StartCoroutine(TypeLine(line));
    }

    private IEnumerator TypeLine(DialogueLine line, float tps = 0.02f){
        lineText.text = "";
        string fullLine = line.sentence;

        foreach (char letter in fullLine.ToCharArray())
        {
            lineText.text += letter;
            
            yield return new WaitForSeconds(tps);
        }
    }

    public void Next(){
        _lineIndex++;
        if (_lineIndex <= _currentNode.lines.Length){
            ShowCurrentLine();
        }
    }
}
