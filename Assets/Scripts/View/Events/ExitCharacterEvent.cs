using System.Collections;
using UnityEngine;

[System.Serializable]
public class ExitCharacterEvent : DialogueEvent
{
    public Characters character;

    public override IEnumerator Execute(DialogueContext context)
    {
        context.characterManager.ExitCharacter(character);
        yield return null;
    }
}
