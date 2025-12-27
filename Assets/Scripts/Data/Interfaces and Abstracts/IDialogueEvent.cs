using System.Collections;

public interface IDialogueEvent
{
    IEnumerator Execute(DialogueContext context);
}
