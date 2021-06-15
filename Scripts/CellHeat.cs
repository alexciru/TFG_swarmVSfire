using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Clase encargada de almacenar informacion de la celda y todos sus componenetes necesarios para
// hacer una pequeña simunlacion de incendio
// TODO: deberian guardas en valor inicial 
public enum Status{ Normal, OnFire, Suffocated, Nule }; // estados de la celda

public class CellHeat{ 

    public Vector3 worldPosition; //posicion en el mundo
    public int gridX;  // informacion sobre la posicion en el grid
    public int gridY;

    public Status status;    // estado de la celda
    public GameObject FireParticle;
    private GameObject instancia_fuego;

    // variables de combustion
    public float wood;
    public float temp = 310f;
    public float new_temp;
    public float new_wood;

    //TODO: seguramente se puede mejorara el rendimiento quitando esto
    public float ambient_loss = 0f;
    public float wind_loss = 0f;
    public float combustion = 0f;
    public float difusion = 0f;
   
    //TODO: comprobar si se necesitan estas variables del constructor
    public CellHeat(Vector3 worldPosition, int gridX, int gridY, GameObject FireParticle){
        this.worldPosition = worldPosition;
        this.gridX = gridX;
        this.gridY = gridY;
        this.FireParticle = FireParticle;

        status = Status.Normal;
    }

    // Funcion que se encarga de lanzar un Raycast hacia abajo y
    // coloca el grid a esa altura
    public void placeCell(){
        RaycastHit hit;
        if(Physics.Raycast(worldPosition, Vector3.down, out hit, Mathf.Infinity)&& hit.transform.gameObject.tag == "Terrain"){
            this.worldPosition =  new Vector3(worldPosition.x, worldPosition.y - hit.distance, worldPosition.z);
        }
    }

    // Actualizamos los valores
    public void update_value(){
        wood = new_wood;
        temp = new_temp;
    }

    public void SpawnParticle(){
        
        instancia_fuego = Object.Instantiate(FireParticle, worldPosition, Quaternion.identity);
        instancia_fuego.transform.parent = GameObject.Find("TrainingTerrain").transform.Find("Particulas");
        return;
    }

    public void DestroyParticle(){    
        UnityEngine.Object.Destroy(instancia_fuego);
        return;
    }

    public bool IsCellOnFire(){
        return (status == Status.OnFire);
    }

    //Elimina la particula
    public void resetCell(float temp, float wood){
        status = Status.Normal;
        this.temp = temp;
        this.wood = wood;
        DestroyParticle();
        return;
    }

}
