using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class flechaVientoScript : MonoBehaviour
{
    // Funcion que se encarga de girar la flecha segun la direcion del viento del scrip de transferencia de calor
    // TODO: utilizar viento del gameobject ¿?¿?¿ 
    
    public float smooth = 5f;
    void Start(){   
        Vector2 wind = GameObject.Find("Terrain").GetComponent<gridHeat>().getWind();;
        
        float wind_x = wind.x;
        float wind_y = wind.y;

        //TODO: comprobar si funciona
        transform.rotation = Quaternion.Euler(0f, -Vector2.Angle(new Vector2(wind_x, wind_y), Vector2.down) + 90 , 0f);
    }
}
