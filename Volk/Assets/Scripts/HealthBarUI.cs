using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public Fighter target;
    public Slider slider;
    public Image fillImage;

    private float displayValue;

    void Start()
    {
        if (target == null) return;
        displayValue = target.currentHP / target.maxHP;
        if (slider) slider.value = displayValue;
    }

    void Update()
    {
        if (target == null || slider == null) return;

        float targetValue = target.currentHP / target.maxHP;
        displayValue = Mathf.Lerp(displayValue, targetValue, Time.deltaTime * 8f);
        slider.value = displayValue;

        if (fillImage)
        {
            fillImage.color = Color.Lerp(Color.red, Color.green, displayValue);
        }
    }
}
