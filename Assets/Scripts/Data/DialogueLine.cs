
public enum Emotion{
    Neutral,
    Happy,
    Angry
}

[System.Serializable]
public class DialogueLine
{
    public string characterName;
    public string sentence;
    public Emotion emotion;
}
