using UnityEngine;
using System.Collections.Generic;

public class CharacterManager : MonoBehaviour
{
    public CharacterDatabase database;

    public CharacterSlot leftSlot;
    public CharacterSlot rightSlot;
    public CharacterSlot centerSlot;

    private Dictionary<Characters, CharacterSlot> activeCharacters =
        new Dictionary<Characters, CharacterSlot>();

    public static CharacterManager Instance;

    private void Awake()
    {
        if (Instance == null){
            Instance = this;
        }
        else{
            Destroy(gameObject);
        }
        SyncFromScene();
    }

    // Считываем начальное состояние сцены
    private void SyncFromScene()
    {
        activeCharacters.Clear();

        if (leftSlot.image.enabled)
            activeCharacters[leftSlot.currentCharacter] = leftSlot;

        if (rightSlot.image.enabled)
            activeCharacters[rightSlot.currentCharacter] = rightSlot;

        if (centerSlot.image.enabled)
            activeCharacters[centerSlot.currentCharacter] = centerSlot;

        Debug.Log(leftSlot.image.enabled);
    }

    public void ApplyLine(DialogueLine line)
    {
        ShowCharacter(line.characterName, line.emotion);
    }

    public void ShowCharacter(Characters character, Emotion emotion)
    {
        // Персонаж уже на сцене → меняем эмоцию
        if (activeCharacters.TryGetValue(character, out var slot))
        {
            SetEmotion(slot, character, emotion);
            return;
        }

        // На сцене НИКОГО → центр
        if (activeCharacters.Count == 0)
        {
            PlaceCharacter(character, emotion, centerSlot);
            return;
        }

        // На сцене ОДИН персонаж
        if (activeCharacters.Count == 1)
        {
            // Если он в центре — сдвигаем влево
            if (centerSlot.image.enabled)
                MoveCenterToLeft();

            PlaceCharacter(character, emotion, rightSlot);
            return;
        }

        // Уже два персонажа → просто меняем эмоцию активному
        Debug.LogWarning("Max 2 characters on screen. Ignoring new character.");
    }

    private void PlaceCharacter(Characters character, Emotion emotion, CharacterSlot slot)
    {
        var data = database.GetCharacter(character);
        if (data == null) return;

        slot.currentCharacter = character;
        slot.SetSprite(data.GetSprite(emotion));
        activeCharacters[character] = slot;
    }

    private void SetEmotion(CharacterSlot slot, Characters character, Emotion emotion)
    {
        var data = database.GetCharacter(character);
        if (data == null) return;

        slot.SetSprite(data.GetSprite(emotion));
    }

    private void MoveCenterToLeft()
    {
        var character = centerSlot.currentCharacter;
        var sprite = centerSlot.image.sprite;

        centerSlot.Clear();

        leftSlot.currentCharacter = character;
        leftSlot.SetSprite(sprite);

        activeCharacters[character] = leftSlot;
    }

    public void ClearAll()
    {
        leftSlot.Clear();
        rightSlot.Clear();
        centerSlot.Clear();
        activeCharacters.Clear();
    }
}