using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BateryUI : MonoBehaviour{
    public Slider slider;
    public Gradient gradient;
    public Image fill;
    public Image chargingIcon;
    public float maxBatery;
    // Start is called before the first frame update
    public void setMaxBatery(float maxValue){
        slider.maxValue = maxValue;
        slider.value = maxValue;
        fill.color = gradient.Evaluate(1f);
    }
    
    public void setBatery(float batery){
        slider.value = batery;
        fill.color = gradient.Evaluate(slider.normalizedValue);
    }
    public void activateChargingIcon(){
        chargingIcon.gameObject.SetActive(true);
    }

    public void deactivateChargingIcon(){
        chargingIcon.gameObject.SetActive(false);
    }
 
}