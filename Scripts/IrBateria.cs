using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using System;


///<sumary>
/// Script agente que interactua con mlAgent y conececta con otros scripts
/// actualmente se encarga del behaviour de 
///</sumary>
public class IrBateria : Agent {

    // Scripts  
    public GameObject trainingTerrain;
    private gridHeat gridHeat;
    private DroneMovement droneMovement;
    private List<Transform> chargeStationList;
    private FusionModule fusionModule;
    private planningModule planningModule;

    // vector hacia el ultimo fuego encontrado
    private Transform nearestChargeStation;

    private Vector3 nearestChargeStationPosition;
    private Vector3 nearestFirePosition;

    private float distanceToFire;
    private float distanceToCharger;
    private bool visitedStation=false; //variable qeu lleva el contro de si ha cargado la bateria
    

    bool DroneReadyToTrain;

    /// For training this behaviour
    public bool actionToDirectMovement = true;
    public bool UseLocalOnEpisodeBegin = true;
    public float activity; //Valor entre 0 y 1 para el IB2C

    ///<sumary>
    /// Inicializa el agente al empezar el entrenamiento
    /// Es utilizada para obtener los componentes del drone
    ///</sumary>
    public override void Initialize(){
        if(trainingTerrain == null) print("ERROR: Campo de entrenamiento no asignado al agente");
        Debug.Log("Ir bateria Inicializado");
        droneMovement = transform.parent.transform.GetComponent<DroneMovement>();
        gridHeat = trainingTerrain.GetComponent<gridHeat>();
        fusionModule = transform.parent.GetComponent<FusionModule>();
        chargeStationList = gridHeat.findChargeStation();
        planningModule = transform.parent.GetComponent<planningModule>();
        
    }
    
    ///<sumary>
    /// Accion que se realiza unicamente al principio de cada episodio de entrenamiento.
    /// En este caso se colocara el dron o drones en una posicion valida y TODO: resetear el mapa
    ///</sumary>
    public override void OnEpisodeBegin(){
        if(UseLocalOnEpisodeBegin){
            DroneReadyToTrain = false;
            gridHeat.updateFireValue = false; //inidicamos que no queremos actualizar el incendio
            droneMovement.ResetRigidBody();
            droneMovement.initiliazeBatery(50f);
            // recolocar los cargadores por el borde del mapa
            moveChargeStationsToRandomPos();
            nearestChargeStation = chargeStationList[0];
            gridHeat.resetGridHeat();
            nearestFirePosition = gridHeat.getInitialFirePosition3D()+Vector3.up*20;
            nearestChargeStationPosition = chargeStationList[0].position+Vector3.up*20;
            updateNearestChargeStation();
            UpdateNearestPerceivedFire();
            distanceToCharger = 999;
            visitedStation=false;
            activity = 1;
            DroneReadyToTrain = true; //control para no activar rewards si no esta colocado los drones        
        }
    }

    ///<sumary>
    /// Se llama cuando se recibe una instruccio o del jugador o de la red neuronal.
    /// para el drone solo podra moverse en el espacio 2D y rotar.
    /// index 0: move vector x  -> (+1 = right, -1 = left)
    /// index 1: move vector z  -> (+1 forward, -1 = backward)
    /// index 2: rotation (yaw) -> (-1 left  --- +1 right)
    ///</sumary>
    public override void OnActionReceived(float[] vectorAction){
        Vector3 move = new Vector3(vectorAction[0], 0, vectorAction[1]);
        if(activity == 1){
            if(vectorAction[2] < 0f){
                if(droneMovement.bateryValue < 30f){
                    AddReward(-0.0001f /(droneMovement.bateryValue));
                }
                move = Vector3.zero; // activamos para quie se quede quieto
            }
            if(actionToDirectMovement){
                droneMovement.UpdateMovement(move);
            }else{
                fusionModule.velocities[1] = move; 
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
        // posicion en el mapa 
        sensor.AddObservation(transform.localPosition.x);
        sensor.AddObservation(transform.localPosition.y);

        //distance to fire
       
        sensor.AddObservation(nearestFirePosition - transform.position);
        //sensor.AddObservation(Vector3.Distance(nearestFirePosition, transform.position));
         //añadir bateria
        sensor.AddObservation(droneMovement.bateryValue / droneMovement.maxBatery);
        //distance to station
        sensor.AddObservation(nearestChargeStationPosition - transform.position);
        //sensor.AddObservation(Vector3.Distance(nearestChargeStationPosition, transform.position));
        //sensor.AddObservation(planningModule.SensorNearestFirePosition - transform.position);
        //sensor.AddObservation(droneMovement.bateryValue / droneMovement.maxBatery);
        //sensor.AddObservation(planningModule.SensornearestChargeStationPosition - transform.position);
    }

    
    // posicionamos las estaciones de carga de manera aleatoria por el borde del mapa
    // En caso de usar curricullum vamos a ir incrementntandolo con el tiempo "chargerDistance"
    // 
    private void moveChargeStationsToRandomPos(){
        int randX, randY;
        int gridSizeX = trainingTerrain.GetComponent<gridHeat>().getGridSizeX();
        int gridSizeY = trainingTerrain.GetComponent<gridHeat>().getGridSizeY();
        int defaultDistance = 2;
        int distanceCharger = Mathf.RoundToInt(Academy.Instance.EnvironmentParameters.GetWithDefault("chargerDistance", defaultDistance ));
        
        int randAux = UnityEngine.Random.Range(0, 1);
        for(int i = 0; i< chargeStationList.Count; i++){
            randAux = UnityEngine.Random.Range(0, 2);
            if(randAux == 0){
                randX = UnityEngine.Random.Range(0, 2) == 0 ? distanceCharger : gridSizeX-distanceCharger;
                randY = UnityEngine.Random.Range(distanceCharger, gridSizeY-distanceCharger);
            }else{
                randX = UnityEngine.Random.Range(distanceCharger, gridSizeX-distanceCharger);
                randY = UnityEngine.Random.Range(0, 2) == 0 ? distanceCharger: gridSizeY-distanceCharger;
            }
            chargeStationList[i].transform.position = trainingTerrain.GetComponent<gridHeat>().WorldPointFromCell(new Vector2(randX, randY));
        }
    }

    ///<sumary>
    ///  se llama cada 1/5 de frame. Es utilizada para las funciones de reward y actualizar sensores
    ///</sumary>
    private void FixedUpdate(){
        if(DroneReadyToTrain){
            //Debug.DrawLine(transform.position, nearestFirePosition, Color.red);
            //Debug.DrawLine(transform.position, nearestChargeStationPosition, Color.green);
            nearestFirePosition = gridHeat.getInitialFirePosition3D()+Vector3.up*20;
            //nearestChargeStationPosition = nearestChargeStation.position;
            droneMovement.SoftdrainBatery();
            updateNearestChargeStation();
            CheckIfOutoffMap();
            GoingToChargeStation();
            GoingTofire();
            CheckBateryNotEmpty();
            //CheckIfLowBatry();
            //CheckIfNearFire();
            //AddReward(-0.00001f);
        }
    }

    private void activationFuntion(){
        //activity = Mathf.InverseLerp(0f, droneMovement.maxBatery , droneMovement.bateryValue); 
        activity = planningModule.stimulous[1];
    }

 
/*----------------------------------------------------------------------------------------
 *                              COLLIDERS Y TRIGGUERS 
 *  
 *   
 *----------------------------------------------------------------------------------------*/
    ///<sumary>
    /// Funcion que se activa la primera vez qeu hay colision de colliders
    ///</sumary>
    public void OnTriggerEnter(Collider other){
        TriggerEnterOrStay(other);
    }

    ///<sumary>
    /// Funcion que se activa cuando se encuentra el collider dentro de otro
    ///</sumary>
    private void OnTriggerStay(Collider other){
        TriggerEnterOrStay(other);
    }

    ///<sumary>
    /// Funcion propia para contar el contacto con el dron
    ///</sumary>
    private void TriggerEnterOrStay(Collider other){
        if(other.CompareTag("drone")){
            AddReward(-.2f);
        }else if(other.CompareTag("charger")){
            if(visitedStation == false){
                
                droneMovement.charging();
                if(droneMovement.bateryValue < 99){
                    AddReward(.0005f);
                    //Debug.log(".0005");
                }
                if(droneMovement.bateryValue <= 99 ){
                    AddReward(1f);
                    //Debug.log("1");
                    distanceToFire = Vector3.Distance(transform.position, nearestFirePosition);
                    visitedStation = true;
                }

            }
        }
    }
    
/*----------------------------------------------------------------------------------------
 *                                     REWARDS 
 *  
 *   
 *----------------------------------------------------------------------------------------*/

    ///<sumary>
    /// Comprueba en que celda esta, si no esta en ninguna celda se encuentra fuera de
    /// terreno. Usamos el velctor -999 como vector null.
    ///</sumary>
    private void CheckIfOutoffMap(){
        Vector2 indexPosition = trainingTerrain.GetComponent<gridHeat>().CellFromWorldPoint(transform.position);
        if(indexPosition.x <= -999){
            AddReward(-100f);
            
            EndEpisode();
        }
        return;
    }


    ///<sumary>
    /// Comprueba cual es la estacion de carga mas cercana que tiene y la actualiza apra el sensor
    ///</sumary>
    private void updateNearestChargeStation(){
        Vector3 potentialStationPosition;
        foreach(Transform station in chargeStationList){
            potentialStationPosition = station.transform.position+Vector3.up*20;
            if(Vector3.Distance(transform.position, potentialStationPosition) < Vector3.Distance(transform.position, nearestChargeStationPosition)){
                nearestChargeStation = station;
                nearestChargeStationPosition = nearestChargeStation.transform.position + Vector3.up*20;
            }
        }
    }


    ///<sumary>
    /// Si se implementa lo de la percecion del fuego se puede usar para actualizar el ultimo fuego percibido
    /// TODO: esto seguramente se pueda hacer mas bonito
    ///</sumary>
    public void UpdateNearestPerceivedFire(){
        Vector2 indexPosition = gridHeat.CellFromWorldPoint(transform.position);
        Vector3 potentialNearestFirePosition;
        int x = (int)indexPosition.x;
        int y = (int)indexPosition.y;
        //Comprobamos las celdas que puede observar el dron
        for(int i=x-3; i<x+3; i++){
            for(int j=y-3; j<y+3 ; j++){
                if(gridHeat.isCellOnFire(i, j)){
                    potentialNearestFirePosition = gridHeat.getCellheat(i, j).worldPosition+Vector3.up*20;
                    if(Vector3.Distance(transform.position, potentialNearestFirePosition) < Vector3.Distance(transform.position, nearestFirePosition)){
                        nearestFirePosition = potentialNearestFirePosition;
                    }
                }
            }
        }
        return;
    }



    ///<sumary>
    /// Comprueba si se queda sin bateria, si es así le penaliza y acaba el episodio
    ///</sumary>
    private void CheckBateryNotEmpty(){
        if(droneMovement.bateryValue <= 0){
            AddReward(-20f);
            //Debug.log("-20");
            EndEpisode();
        }
    }




    ///<sumary>
    /// Le fomentamos cuando se dirije hacia la estacion de carga con poca carga
    ///</sumary>
    private void GoingToChargeStation(){
        float actualDistanceToStation = Vector3.Distance(transform.position, nearestChargeStationPosition);
        float distanceDiffToStation = distanceToCharger - actualDistanceToStation;
        //print("distance: " + actualDistanceToStation);
        //print("diff: " + distanceDiffToStation);
        //print(distanceDiff);
        //TODO: cambiar condicion mas generica, no poner valor como tal
        if(distanceDiffToStation > 2 && visitedStation == false){
            AddReward(0.5f);
            //Debug.log("0.2f");
            distanceToCharger = actualDistanceToStation;
        }
    }


    ///<sumary>
    /// Le fomentamos cuando se dirije hacia el fuego con mucha carga
    ///</sumary>
    private void GoingTofire(){
        float actualDistanceToFire = Vector3.Distance(transform.position, nearestFirePosition);
        float distanceDiff = distanceToFire - actualDistanceToFire;
        
        if(distanceDiff > 2 && visitedStation == true){
            distanceToFire = actualDistanceToFire;
            
            AddReward(0.5f);
            //Debug.log("0.2");
        }
        if(actualDistanceToFire <= 3 && visitedStation == true){
            AddReward(0.5f * droneMovement.bateryValue);     
            //Debug.log(0.1*droneMovement.bateryValue);       
            EndEpisode();
        } 

    }

     // Fucion qeu comprueba la distacia con el ultimo fuego detectado
    // y otorga un reward acorde
    void CheckIfNearFire(){
        //Vector3 proyectedPos = new Vector3(nearestFirePosition.x, transform.position.y, nearestFirePosition.z);
        if(Vector3.Distance(transform.position, nearestFirePosition) < 12){
            AddReward(0.00001f * (droneMovement.bateryValue - 30f));
        }

    }

    void CheckIfLowBatry(){
        //Vector3 proyectedPos = new Vector3(nearestFirePosition.x, transform.position.y, nearestFirePosition.z);
        if(droneMovement.bateryValue < 30f ){
            AddReward(-0.0001f /(droneMovement.bateryValue));            
        }

    }
}
