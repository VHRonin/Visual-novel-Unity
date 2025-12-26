using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Scriptable Objects/CharacterData")]
public class CharacterData : ScriptableObject
{
    public Characters character;   // enum персонажей
    public string displayName;
    public List<EmotionSprite> emotions;

    public Sprite GetSprite(Emotion emotion)
    {
        foreach (var e in emotions)
        {
            if (e.emotion == emotion)
                return e.sprite;
        }

        Debug.LogWarning($"[{name}] Emotion {emotion} not found");
        return null;
    }
}
