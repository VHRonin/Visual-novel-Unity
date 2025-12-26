using UnityEngine;

public class ChoiceData : MonoBehaviour
{
    public string nextId;
    [SerializeField] private DialogueSystem dialogueSystem;

    private void Start() {
        dialogueSystem = FindAnyObjectByType<DialogueSystem>();
    }

    public void OnChoiceSelected(){
        dialogueSystem.OnChoiceSelected(this.nextId);
    }
}
