using System.Collections;
using UnityEngine;

[System.Serializable]
public abstract class DialogueEvent : IDialogueEvent
{
    public abstract IEnumerator Execute(DialogueContext context);
}
