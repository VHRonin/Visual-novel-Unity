
using UnityEngine;


[System.Serializable]
public class DialogueLine
{
    // public string characterName;
    public Characters characterName;
    [TextArea(3, 5)] public string sentence;
    public Emotion emotion;
}
