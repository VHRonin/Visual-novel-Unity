using UnityEngine;
using UnityEngine.UIElements;
 
public class GameMenuController : MonoBehaviour
{
    private int hp = 85;
    private int mp = 42;
 
    private const int MaxHp = 100;
    private const int MaxMp = 60;
 
    private VisualElement hpFill;
    private VisualElement mpFill;
 
    private Label hpText;
    private Label mpText;
    private Label status;
 
    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
 
        hpFill = root.Q<VisualElement>("hp-fill");
        mpFill = root.Q<VisualElement>("mp-fill");
 
        hpText = root.Q<Label>("hp-text");
        mpText = root.Q<Label>("mp-text");
        status = root.Q<Label>("status");
 
        root.Q<Button>("continue-btn").clicked += () =>
        {
            status.text = "Your journey continues...";
            Debug.Log("Continue clicked");
        };
 
        root.Q<Button>("rest-btn").clicked += Rest;
 
        root.Q<Button>("exit-btn").clicked += () =>
        {
            status.text = "Leaving the adventure...";
            Debug.Log("Exit clicked");
 
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        };
 
        UpdateStats();
    }
 
    private void Rest()
    {
        if (hp == MaxHp && mp == MaxMp)
        {
            status.text = "You are already fully rested.";
            return;
        }
 
        hp = MaxHp;
        mp = MaxMp;
 
        UpdateStats();
        status.text = "HP and MP fully restored!";
    }
 
    private void UpdateStats()
    {
        hpText.text = $"{hp} / {MaxHp}";
        mpText.text = $"{mp} / {MaxMp}";
 
        hpFill.style.width =
            Length.Percent((float)hp / MaxHp * 100f);
 
        mpFill.style.width =
            Length.Percent((float)mp / MaxMp * 100f);
    }
}