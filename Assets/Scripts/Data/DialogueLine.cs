
public enum Emotion{
    Neutral,
    Happy,
    Angry
}

public enum Characters
{
    Астра,
    Сигурд
}


[System.Serializable]
public class DialogueLine
{
    // public string characterName;
    public Characters characterName;
    public string sentence;
    public Emotion emotion;
}
