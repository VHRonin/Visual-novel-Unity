using System.Collections;
using UnityEngine;

[System.Serializable]
public class SayEvent : DialogueEvent
{
    public Characters character;
    [TextArea(3, 5)] public string text;
    public Emotion emotion;

    public override IEnumerator Execute(DialogueContext context)
    {
        // context.characterManager.ShowCharacter(character, emotion); // эту строчку нужно удалить, чтобы контррлировать всё через EnterCharacter и ExitCharacter

        yield return context.dialogueUI.ShowLine(character, text);
    }
}
