using System.Collections;
using TMPro;
using UnityEngine;

public class DialogueUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text lineText;
    [SerializeField] private GameObject nameplate;
    [SerializeField] private GameObject continueIcon;
    private bool waitingForInput;
    private Coroutine typingCoroutine;
    private bool isTyping;
    private bool skipTying;

    public IEnumerator ShowLine(Characters character, string sentence, float tps = 0.02f)
    {
        if (character != Characters.Рассказчик){
            nameplate.SetActive(true);
            nameText.text = character.ToString();
        }
        else {
            nameplate.SetActive(false);
        }
        

        isTyping = true;
        skipTying = false;

        // if (typingCoroutine != null)
        //     StopCoroutine(typingCoroutine);

        // typingCoroutine = StartCoroutine(TypeLine(sentence, tps));

        yield return StartCoroutine(TypeLine(sentence, tps));

        isTyping = false;
        ShowContinueIcon(true);

        waitingForInput = true;
        while (waitingForInput)
            yield return null;
    }

    private IEnumerator TypeLine(string sentence, float tps)
    {
        lineText.text = "";

        foreach (char letter in sentence)
        {
            if (skipTying){
                lineText.text = sentence;
                break;
            }
            lineText.text += letter;
            yield return new WaitForSeconds(tps);
        }
    }

    public void OnNextClicked()
    {
        if (isTyping){
            skipTying = true;
            return;
        }
        ShowContinueIcon(false);
        waitingForInput = false;
    }

    private void ShowContinueIcon(bool show){
        if (continueIcon != null){
            continueIcon.SetActive(show);
        }
    }
    // public IEnumerator TypeLine(DialogueLine line, float tps = 0.02f){
    //     lineText.text = "";
    //     string fullLine = line.sentence;

    //     foreach (char letter in fullLine.ToCharArray())
    //     {
    //         lineText.text += letter;
            
    //         yield return new WaitForSeconds(tps);
    //     }
    // }
}
