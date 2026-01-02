using UnityEngine;
using UnityEngine.UI;

public class CharacterSlot : MonoBehaviour
{
    public Image image;
    public Characters currentCharacter;
    public RectTransform visualRoot;
    public float targetHeight = 2048f;

    public void SetSprite(Sprite sprite, CharacterData data)
    {
        image.sprite = sprite;
        image.enabled = sprite != null;

        float scale = targetHeight / data.baseHeight;
        visualRoot.localScale = Vector3.one * scale;

        visualRoot.anchoredPosition = data.bodyOffset * scale;
    }

    public void Clear()
    {
        currentCharacter = default;
        image.sprite = null;
        image.enabled = false;
    }
}
