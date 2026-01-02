using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Scriptable Objects/CharacterData")]
public class CharacterData : ScriptableObject
{
    public Characters character;
    public string displayName;
    public List<EmotionSprite> emotions;
    public float baseHeight;
    public Vector2 bodyOffset;

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
