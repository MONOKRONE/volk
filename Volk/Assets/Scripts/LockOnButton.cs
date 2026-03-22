using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LockOnButton : MonoBehaviour
{
    public Fighter playerFighter;
    public Image buttonImage;
    public TextMeshProUGUI buttonText;

    public Color lockedColor = new Color(0.9f, 0.72f, 0f);
    public Color unlockedColor = new Color(0.2f, 0.2f, 0.2f);
    public Color lockedTextColor = new Color(0.08f, 0.08f, 0.08f);
    public Color unlockedTextColor = new Color(0.7f, 0.7f, 0.7f);

    void Start()
    {
        UpdateVisual();
        GetComponent<Button>().onClick.AddListener(Toggle);
    }

    void Toggle()
    {
        if (playerFighter == null) return;
        playerFighter.lockOnEnabled = !playerFighter.lockOnEnabled;
        UpdateVisual();
    }

    void UpdateVisual()
    {
        bool locked = playerFighter != null && playerFighter.lockOnEnabled;
        if (buttonImage) buttonImage.color = locked ? lockedColor : unlockedColor;
        if (buttonText)
        {
            buttonText.text = locked ? "LOCK" : "FREE";
            buttonText.color = locked ? lockedTextColor : unlockedTextColor;
        }
    }
}
