using UnityEngine;

public class NextButton : MonoBehaviour
{
    [SerializeField] private DialogueUI dialogueUI;

    public void OnNextClicked(){
        dialogueUI.OnNextClicked();
    }
}
