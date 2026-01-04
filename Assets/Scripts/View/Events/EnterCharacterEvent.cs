using System.Collections;
using UnityEngine;

public class EnterCharacterEvent : DialogueEvent
{
    public Characters character;
    public Emotion emotion;
    public CharacterSlotType slotType;
    public override IEnumerator Execute(DialogueContext context)
    {
        context.characterManager.EnterCharacter(character, emotion, slotType);

        yield return null;
    }
}
