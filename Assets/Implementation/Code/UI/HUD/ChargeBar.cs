using UnityEngine;
using UnityEngine.UI;

public class ChargeBar : MonoBehaviour
{
    public Slider slider;

    // We pass in a value between 0.0 and 1.0
    public void UpdateBar(float fillPercentage)
    {
        if (slider == null)
        {
            return;
        }

        slider.value = fillPercentage;
    }

    public void ResetBar()
    {
        if (slider == null)
        {
            return;
        }

        slider.value = 0f;
    }
}
