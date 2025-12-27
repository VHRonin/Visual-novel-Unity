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
    [SerializeField] private DialogueUI _dialogueUI;
    private DialogueContext _context;

    [Header("Choices settings")]
    [SerializeField] private GameObject _choicesPanel;
    [SerializeField] private Transform _choicesContainer;
    [SerializeField] private GameObject _choiceButtonPrefab;

    private void Awake() {
        _context = new DialogueContext {
            characterManager = CharacterManager.Instance,
            dialogueUI = _dialogueUI
        };
    }

    private void Start() {
        _currentNode = GetNode("start");
        _lineIndex = 0;
        ShowCurrentLine();
    }

    private DialogueNode GetNode(string nodeID){
        var node = Array.Find(dialogues.dialogueNodes, n => n.id == nodeID);

        return node;
    }

    private IEnumerator RunNode(DialogueNode node){
          foreach (var dialogueEvent in node.events)
        {
            yield return dialogueEvent.Execute(_context);
        }

        if (node.choices != null && node.choices.Length > 0)
            ShowChoices();
    }

    private void ShowCurrentLine(){
        if (_lineIndex >= _currentNode.lines.Length){
            if (!_choicesPanel.activeInHierarchy){
                ShowChoices();
            }
            return;
        }
        var line = _currentNode.lines[_lineIndex];
        CharacterManager.Instance.ApplyLine(line);
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

    private void ShowChoices(){
        _choicesPanel.SetActive(true);
        
        for (int i = 0; i< _currentNode.choices.Length; i++){
            GameObject choiceButton = Instantiate(_choiceButtonPrefab, _choicesContainer);
            TMP_Text buttonText = choiceButton.transform.GetChild(0).GetComponent<TMP_Text>();
            buttonText.text = _currentNode.choices[i].choiceText;
            choiceButton.GetComponent<ChoiceData>().nextId = _currentNode.choices[i].nextId;
        }
    }

    public void OnChoiceSelected(string nextId){
        _currentNode = GetNode(nextId);
        _lineIndex = 0;

        for (int i = 0; i < _choicesContainer.childCount; i++){
            Destroy(_choicesContainer.GetChild(i).gameObject);
        }

        _choicesPanel.SetActive(false);

        ShowCurrentLine();
    }
}
