using System.Collections;
using TMPro;
using UnityEngine;

public class DialogueUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text lineText;
    private bool waitingForInput;
    private Coroutine typingCoroutine;

    public IEnumerator ShowLine(Characters character, string sentence, float tps = 0.02f)
    {
        nameText.text = character.ToString();

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeLine(sentence, tps));

        waitingForInput = true;
        while (waitingForInput)
            yield return null;
    }

    private IEnumerator TypeLine(string sentence, float tps)
    {
        lineText.text = "";

        foreach (char letter in sentence)
        {
            lineText.text += letter;
            yield return new WaitForSeconds(tps);
        }
    }

    public void OnNextClicked()
    {
        waitingForInput = false;
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
