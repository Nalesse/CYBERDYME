using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;

    public void SetMaxHealthUI(int health)
    {
        slider.maxValue = health;
        slider.value = health;
    }

    public void SetHealthUI(int health)
    {
        slider.value = health;
    }

}