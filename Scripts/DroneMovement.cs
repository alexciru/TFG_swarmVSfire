using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;


///<sumary>
/// Script encargado del movimiento y colocación de los drones. Ademas de las fisicas
/// y de la bateria.
///</sumary>
public class DroneMovement : MonoBehaviour{   

    new private Rigidbody rigidbody;
    public Camera agentCamera;
    
    public float moveForce = 20f;
    
    public float bateryValue = 100;
    public float maxBatery = 100;
    public bool bateryActive = false;
    private BateryUI bateryUI;

    public GameObject trainingTerrain;
    
    void Start(){
        rigidbody = transform.parent.transform.GetComponent<Rigidbody>();
        trainingTerrain = transform.parent.transform.parent.gameObject;
        bateryUI = transform.parent.transform.transform.Find("Canvas").transform.Find("BateryBar").GetComponent<BateryUI>();
        bateryUI.setMaxBatery(maxBatery);
        
        if(bateryActive){
            transform.parent.transform.Find("Canvas").gameObject.SetActive(true);
        }else{
            transform.parent.transform.Find("Canvas").gameObject.SetActive(false);
        }
    }



    public void UpdateMovement(Vector3 move){
        if(bateryValue <= 0f) return; // si esta congelado no tomar accion
        
        /*
        if(vectorAction.Length == 0){
            print("vector accion a 0");
            return;
        } 
        // Vector velocidad: calcula vector de movimiento y aplicamos
        Vector3 move = new Vector3(vectorAction[0], 0, vectorAction[1]);*/
        
        if(move != Vector3.zero) DrainBatery();
        rigidbody.AddForce(move*moveForce);
    }

    public void ResetRigidBody(){
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
    }


    // --------------------------------- Bateria --------------------------------------
    
    public void initiliazeBatery(float value ){
        if(bateryActive){
            bateryValue = UnityEngine.Random.Range(value, 100f);
        }else{
            bateryValue = 100f;
        }
        bateryUI.setBatery(bateryValue);
    }


    // De momento ponemos que cada segundo quita bateria,
    // TODO: cambiar la relacion de velocidad con bateria y con movimeino diagonal
    // esta funcion se llama en conjunto con softdrain por eso le ponemos un valor mas bajito
    public void DrainBatery(){
        if(bateryActive){
            bateryValue -= 0.01f;
            bateryUI.setBatery(bateryValue);
        }
    }

    // De momento ponemos que cada segundo quita bateria,
    // Es utilizado para el entrenaminto del IrBateria
    public void SoftdrainBatery(){
        if(bateryActive){
            bateryValue -= 0.005f;
            bateryUI.setBatery(bateryValue);
        }
    }


    // Utilizamos la colisoin de la estacion de carga 
    public void charging(){
        if(rigidbody.velocity != Vector3.zero){
            bateryValue += 0.2f;
            if(bateryValue > maxBatery) bateryValue = 100;
            bateryUI.setBatery(bateryValue);
        }
    }



    ///<sumary>
    /// Coloca al eagente en una posicion valida. Se llamara al inico de cada episodio.
    /// comprueba que no esta muy cerca del otro dron.
    ///</sumary>
    public void MoveToRandomPosition(Vector2 initialFirePosition){
        transform.parent.position = trainingTerrain.GetComponent<gridHeat>().getWorldBottomLeft(); // les movemos a la esquina de abajo antes de colorcarlo
        bool inFrontOfFire = false;
        bool safePositionFound = false;
        if(trainingTerrain == null) print("ERROR: Terreno de entrenamiento no asignado en move position");
        if(trainingTerrain == null) return;
      
        float rand= UnityEngine.Random.Range(0.0f, 1.0f);
        if(rand >= 0.0f) inFrontOfFire = true;

        // Obtenemos posicion del campo de entrenamiento
        Quaternion potentialRotation;
        Vector3 potentialPosition = Vector3.zero;
        int randX = 0, randY = 0, intentos = 200;

        Vector2 potentialCell;
        
        float sizeX = trainingTerrain.GetComponent<gridHeat>().getGridSizeX();
        float sizeY = trainingTerrain.GetComponent<gridHeat>().getGridSizeY();
        float cellDiameter = trainingTerrain.GetComponent<gridHeat>().getCellDiameter();
        while( !safePositionFound && intentos>0){
            if(inFrontOfFire){
                int angle = UnityEngine.Random.Range(0,360);
                
                randX = (int)Math.Round( initialFirePosition.x + (3.5 * Math.Cos( angle ) ));
                randY = (int)Math.Round( initialFirePosition.y + (3.5 * Math.Sin( angle ) ));
                
                potentialCell = new Vector2(randX, randY);
                potentialPosition = trainingTerrain.GetComponent<gridHeat>().WorldPointFromCell(potentialCell);
                potentialPosition.y = 20;
            }else{
                //posicion aleatoria dentro del margen (0,0 es esquina izquierda)
                randX = (int)UnityEngine.Random.Range(1, sizeX-1);
                randY = (int)UnityEngine.Random.Range(1, sizeY-1);
                potentialCell = new Vector2(randX, randY);
                potentialPosition = trainingTerrain.GetComponent<gridHeat>().WorldPointFromCell(potentialCell);
                potentialPosition.y = 20;
            }
            intentos--;
            float yaw = UnityEngine.Random.Range(-180f, 180f);
            potentialRotation = Quaternion.Euler(0f, yaw, 0f);

            //comprobamos colisiones
            Collider[] colliders = Physics.OverlapSphere(potentialPosition, cellDiameter*2);
            if(colliders.Length == 0){
                safePositionFound = true;
            }else{
                intentos--;
                if(intentos == 0) print("ERROR: no se encontro posicion valida para dron");
            }
            
        }
        //nearestFirePosition = gridHeat.getInitialFirePosition3D();
        transform.parent.transform.position = potentialPosition;
    }


    // Sobrecarga que situa al drone en el medio del terreno
    public void MoveToRandomPosition(){
        float sizeX = trainingTerrain.GetComponent<gridHeat>().getGridSizeX();
        float sizeY = trainingTerrain.GetComponent<gridHeat>().getGridSizeY();
        int randX = (int)UnityEngine.Random.Range((sizeX/2)+1, (sizeX/2)-1);
        int randY = (int)UnityEngine.Random.Range((sizeX/2)+1, (sizeX/2)-1);
        Vector2 potentialCell = new Vector2(randX, randY);
        Vector3 potentialPosition = trainingTerrain.GetComponent<gridHeat>().WorldPointFromCell(potentialCell);
        potentialPosition.y = 20;

        transform.parent.transform.position = potentialPosition;
    }
    

    public void Heuristic(float[] actionsOut){
        Vector3 forward = Vector3.zero;
        Vector3 left = Vector3.zero;
        
        // convierte teclas en movimientos. solo o 1 o 0
        if(Input.GetKey(KeyCode.W)) forward = transform.forward;
        else if(Input.GetKey(KeyCode.S)) forward = -transform.forward;

        if(Input.GetKey(KeyCode.A)) left = -transform.right;
        else if(Input.GetKey(KeyCode.D)) left = transform.right;

        if(Input.GetKey(KeyCode.Space)){
            forward = Vector3.zero;
            left = Vector3.zero;
        };
        /*
        float yaw = 0f;
        if(Input.GetKey(KeyCode.Q)) yaw = -1f;
        else if(Input.GetKey(KeyCode.E)) yaw = 1f;
        //actionsOut[2] = yaw;
        */
        Vector3 combined = (forward + left).normalized;
        actionsOut[0] = combined.x;
        actionsOut[1] = combined.z;
    }


    public Vector3 getRigidbodyvelocity(){
        return rigidbody.velocity;
    }

}
