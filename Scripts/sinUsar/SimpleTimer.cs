
using UnityEngine;
using System.Collections;

public class SimpleTimer: MonoBehaviour {
    public float targetTime = 60.0f;


    public SimpleTimer(float time){
        this.targetTime = Time.deltaTime + time;
    }


    public bool step(){
        //print("Actual: " + targetTime + "  - Delta: " + Time.deltaTime);
        if (Time.deltaTime > targetTime) return true;
        return false;
    }

    public void reset_timer(float time){
        this.targetTime = Time.deltaTime + time;
    }

}
