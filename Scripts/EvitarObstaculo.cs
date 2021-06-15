using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Random=UnityEngine.Random;
using System.Linq;
using System;


///<sumary>
/// Script agente que interactua con mlAgent y conececta con otros scripts
/// actualmente se encarga del behaviour de 
///</sumary>
public class EvitarObstaculo : Agent {

    // Variables GLobales del sistema  
    public GameObject trainingTerrain;
    private gridHeat gridHeat;
    private DroneMovement droneMovement;
    private FusionModule fusionModule;
    private planningModule planningModule;


    public bool DroneReadyToTrain;

    /// For training this behaviour
    public bool actionToDirectMovement = true;
    public bool UseLocalOnEpisodeBegin = true;
    public float activity; //Valor entre 0 y 1 para el IB2C

    private Vector3 initial_posotion;
    private Vector2 ultimaCelda;
    // Model for dodge
    [Header("Modelos que esquivar")]
    public GameObject[] obstaculeModels;

    private List<GameObject> obstacules;
    private List<Vector3> obstaculesDirections;

    private Vector3[] speeds;
    GameObject obstaculesGameObject;


    [Header("Modelos que esquivar")]
    public float modelSpeedMin = 0.03f;
    public float modelSpeedMax = 0.50f;

    RayPerceptionSensorComponent3D  perceptionSensores;

    public float moddelAppearRate = 1f;
    
    ///<sumary>
    /// Inicializa el agente al empezar el entrenamiento
    /// Es utilizada para obtener los componentes del drone
    ///</sumary>
    public override void Initialize(){
        if(trainingTerrain == null) print("ERROR: Campo de entrenamiento no asignado al agente");
        Debug.Log("Evitar Obstaculo Inicializado");
        gridHeat = trainingTerrain.GetComponent<gridHeat>();
        droneMovement = transform.parent.GetComponent<DroneMovement>();
        fusionModule = transform.parent.GetComponent<FusionModule>();
        planningModule = transform.parent.parent.GetComponent<planningModule>();


        obstacules = new List<GameObject>();
        obstaculesDirections = new List<Vector3>();

        obstaculesGameObject = new GameObject("Obstaculos");
        obstaculesGameObject.transform.parent = trainingTerrain.transform;

        
        perceptionSensores = transform.Find("Sensores").GetComponent<RayPerceptionSensorComponent3D>();
        RayPerceptionInput spec = perceptionSensores.GetRayPerceptionInput();
        RayPerceptionOutput obs = RayPerceptionSensor.Perceive(spec);
        Debug.Log(spec);

    }

    ///<sumary>
    /// Accion que se realiza unicamente al principio de cada episodio de entrenamiento.
    /// En este caso se colocara el dron o drones en una posicion valida y TODO: resetear el mapa
    ///</sumary>
    public override void OnEpisodeBegin(){
        if(UseLocalOnEpisodeBegin){
            DroneReadyToTrain = false;
            droneMovement.bateryActive = false;
            gridHeat.updateFireValue = false;
            StopCoroutine("GenerateRandomObstacule");
            ultimaCelda = trainingTerrain.GetComponent<gridHeat>().WorldPointFromCell(transform.position);
            droneMovement.ResetRigidBody();
            removeObstacules();
            droneMovement.MoveToRandomPosition();
            StartCoroutine("GenerateRandomObstacule");
            activity = 1;
            DroneReadyToTrain = true;
        }
    }


    ///<sumary>
    /// Se llama cuando se recibe una instruccio o del jugador o de la red neuronal.
    /// para el drone solo podra moverse en el espacio 2D y rotar.
    /// index 0: move vector x  -> (+1 = right, -1 = left)
    /// index 1: move vector z  -> (+1 forward, -1 = backward)
    ///</sumary>
    public override void OnActionReceived(float[] vectorAction){
        // truncamos el vector
        Vector3 move = new Vector3(vectorAction[0], 0, vectorAction[1]);
        if(vectorAction[2] < 0f){
            move = Vector3.zero; // activamos para quie se quede quieto
        }
        if(actionToDirectMovement){
            
            droneMovement.UpdateMovement(move);
        }else{
            fusionModule.velocities[2] = move; 
            fusionModule.activities[2] = activity;
        }
    }


    ///<sumary>
    /// Corutina que genera un obstaculo aleatorio , en una posicion aleatoria que se dirige hacia el drone
    ///</sumary>
    public IEnumerator GenerateRandomObstacule()
    {   
        float offset;
        int randX, randY;
        int gridSizeX = trainingTerrain.GetComponent<gridHeat>().getGridSizeX();
        int gridSizeY = trainingTerrain.GetComponent<gridHeat>().getGridSizeY();
        //random model
        while(true){
            yield return new WaitForSeconds(moddelAppearRate);
            int randIndex = Random.Range(0, obstaculeModels.Count());

            if(randIndex == 2) offset = 20f; // Si es el drone ajustamos la altura
            else offset = 0;

            
            int randAux = Random.Range(0, 2);
            if(randAux == 0){
                randX = Random.Range(0, 2) == 0 ? 1 : gridSizeX-1;
                randY = Random.Range(1, gridSizeY-1);
            }else{
                randX = Random.Range(1, gridSizeX-1);
                randY = Random.Range(0, 2) == 0 ? 1: gridSizeY-1;
            }

            GameObject obstacule = Instantiate(obstaculeModels[randIndex], gridHeat.WorldPointFromCell(new Vector2(randX, randY)) + new Vector3(0,offset,0), Quaternion.identity);
            //random position
            Vector3 obstaculeDirection =  (transform.position - obstacule.transform.position) ;
            obstacule.transform.parent = obstaculesGameObject.transform;

            obstacules.Add(obstacule);
            obstaculesDirections.Add(Vector3.Normalize(obstaculeDirection));
        }
    }

    ///<sumary>
    /// Funcion que elimina los obstaculos
    ///</sumary>
    public void removeObstacules(){
        foreach(GameObject obstacule in obstacules){
            Destroy(obstacule);
        }
        obstacules = new List<GameObject>();
        obstaculesDirections = new List<Vector3>();
    }



    ///<sumary>
    /// funtion that moves the gameObjects in a direction in order to the drone to dodge
    /// y borra el objeto si se encuentra fuera del mapa
    /// 
    ///</sumary>
    public void moveObstacules(){
        foreach(GameObject obstaculo in obstacules.ToArray()){
            
            obstaculo.transform.position += obstaculesDirections[obstacules.IndexOf(obstaculo)] * modelSpeedMax;

            if(gridHeat.CellFromWorldPoint(obstaculo.transform.position).x == -999){
                obstaculesDirections.RemoveAt(obstacules.IndexOf(obstaculo));
                obstacules.Remove(obstaculo);
                Destroy(obstaculo);
            }

        }

    }

    ///<sumary>
    /// Cuando se pone el behaviou en "heuristics only" esta funcion sera llamada para que
    /// que el jugador tome el control. La salida es el array de acciones en actionsOut.
    /// <see cref="OnActionRecived(float[])"/> instead of using neural network
    ///</sumary>
    public override void Heuristic(float[] actionsOut){
        droneMovement.Heuristic(actionsOut);
    }

    ///<sumary>
    /// Collect vector observation from the envioroment
    /// Posibles sensores:
    ///</sumary>
    public override void CollectObservations(VectorSensor sensor){
        sensor.AddObservation(transform.localPosition.x);
        sensor.AddObservation(transform.localPosition.y);
    }
    
    // se llama cada frame
    // se puede usar para debugear, pintando una linea con la celda mas cercana donde ha encontrado fuego
    private void FixedUpdate(){
        if(DroneReadyToTrain && UseLocalOnEpisodeBegin){
            //activationFuntion();
            AddReward(.001f);
            moveObstacules();
            CheckIfOutoffMap();
            if(droneMovement.getRigidbodyvelocity() == Vector3.zero){
                AddReward(.02f);
            }
        }
         

    }

    // actualiza la funcion d activcion del iB2C
    private void activationFuntion(){
        activity = planningModule.stimulous[0]; 
    }
 
 /*----------------------------------------------------------------------------------------
 *                                COLLIDERS Y TRIGGUERS 
 *  
 *   
 *----------------------------------------------------------------------------------------*/
    ///<sumary>
    /// Funcion que se activa la primera vez qeu hay colision de colliders
    ///</sumary>
    void OnTriggerEnter(Collider other){
        TriggerEnterOrStay(other);
    }

    ///<sumary>
    /// Funcion que se activa cuando se encuentra el collider dentro de otro
    ///</sumary>
    void OnTriggerStay(Collider other){
        TriggerEnterOrStay(other);
    }

    ///<sumary>
    /// Funcion propia para contar el contacto con el dron
    ///</sumary>
    private void TriggerEnterOrStay(Collider other){
        if(other.CompareTag("drone")){
            AddReward(-10f);
            EndEpisode();
        }else if(other.CompareTag("obstaculo")){
            AddReward(-10f);
            EndEpisode();
        }
    }


 
 /*----------------------------------------------------------------------------------------
 *                                       SENSORES 
 *  
 *   En esta parte vamos a obtener los valores de los sensores para pasar a cada uno de lo
 *   Comportamientos.
 *----------------------------------------------------------------------------------------*/
    


/*----------------------------------------------------------------------------------------
 *                                      REWARDS 
 *  
 *   
 *----------------------------------------------------------------------------------------*/

    ///<sumary>
    /// Comprueba en que celda esta, si no esta en ninguna celda se encuentra fuera de
    /// terreno. Usamos el velctor -999 como vector null.
    ///</sumary>
    private void CheckIfOutoffMap(){
        Vector2 indexPosition = gridHeat.CellFromWorldPoint(transform.position);
        if(indexPosition.x <= -999){ //Comprueba di esta fuera del mapa
            AddReward(-10f);
            EndEpisode();
        }
        return;
    }

    ///<sumary>
    /// Comprobamos si cambiamos de celda, si es asi penalizamos al dron por moverse
    ///</sumary>
    void ComprobarCambioCelda(){
        Vector2 pos = gridHeat.WorldPointFromCell(transform.position);
        if(ultimaCelda != pos){
            ultimaCelda = pos;
            AddReward(-0.05f);
        }
    }  
}
