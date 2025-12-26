using UnityEngine;
using UnityEngine.UI;

public class CharacterSlot : MonoBehaviour
{
    public Image image;
    public Characters currentCharacter;

    public void SetSprite(Sprite sprite)
    {
        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    public void Clear()
    {
        currentCharacter = default;
        image.sprite = null;
        image.enabled = false;
    }
}
