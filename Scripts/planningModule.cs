using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Barracuda;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

public enum Tarea
{
    GLOBAL,
    BORDEARFUEGO,
    BATERIA,
    EVITAROBSTACULO

}

///<sumary>
/// Este modulo se encarga de configurar todos los comportamientos marcados para entrenar
/// y preparar el terreno para los requisitos de cada uno de ellos.
/// se encarga de Obtener la infroamacion del entorno y pasarlo a los diferentes comportamientos
/// Para que puedan tomar una decision 
///</sumary>
public class planningModule : Agent{


    [Header("Training Behaviours")]
    public Tarea task;


    [Header("Brains")]
    [HideInInspector] public float[] stimulous;
    bool DroneReadyToTrain;
    
    public NNModel NeuralBordearFuego;
    public NNModel NeuralIrBateria;
    public NNModel NeuralEsquivarObstaculo;

    // VARIABLES DEL CAMPO DE ENTRENAMIENTO
    private GameObject trainingTerrain;
    private gridHeat gridHeat;
    private DroneMovement droneMovement;
    private Transform chargeStationParent;
    private List<Transform> chargeStationList;
    private BeliveMap beliveMap;
  
    private FusionModule fusionModule;
    // VARIABLES DE SENSORES
    [HideInInspector] public Vector3 SensorNearestFirePosition = Vector3.zero;
    [HideInInspector] public float distanceToFire;
    [HideInInspector] public float SensorAngleWithFire, angle;
    [HideInInspector] public Vector3 SensornearestChargeStationPosition;
    [HideInInspector] private Transform nearestChargeStation;
    [HideInInspector] private float SensordistanceToCharger;
    [HideInInspector] private bool nearFire = true;
    [HideInInspector]private bool charging = false;
    private float behaviourActive;
   
    RayPerceptionSensorComponent3D  perceptionSensores;
  
    public float[] activity;



    ///<sumary>
    /// Fucion llamada al empezar la simulacion,
    /// Utilizada para inicializar las variables
    ///</sumary>
    public override void Initialize(){    
        trainingTerrain = transform.parent.gameObject;
        if(trainingTerrain == null) print("ERROR: Campo de entrenamiento no asignado al agente");
        //Inizializamos las variables del sistema
        Debug.Log("Inicializando planning");
        activity = new float[3];
        activity[0] = 0f;
        activity[1] = 0f;
        activity[2] = 0f;
        perceptionSensores = transform.Find("Behaviours").Find("EvitarObstaculo").Find("Sensores").GetComponent<RayPerceptionSensorComponent3D>();
        gridHeat = trainingTerrain.GetComponent<gridHeat>();
        chargeStationParent = transform.parent.Find("Estaciones").transform;
        chargeStationList = gridHeat.findChargeStation(); 
        droneMovement = transform.Find("Behaviours").GetComponent<DroneMovement>();
        beliveMap = transform.parent.Find("BeliveMap").GetComponent<BeliveMap>();
        configure();
        stimulous = new float[3];
    }


    ///<sumary>
    /// LLamada para activar y desactivar los difernetes elementos segun queramos entrenar 
    /// o utilizar el modulo de planning
    ///</sumary>
    void configure(){
        Debug.Log("Planning activado");
        switch(task){
            case Tarea.GLOBAL:
                chargeStationParent.gameObject.SetActive(true);
                gridHeat.updateFireValue = true;
                Debug.Log("Trea.global activado");
                transform.Find("Behaviours").GetComponent<FusionModule>().enabled = true;

                transform.Find("Behaviours").GetComponent<DroneMovement>().bateryActive = true;

                transform.Find("Behaviours").Find("BordearFuego").gameObject.SetActive(true);
                transform.Find("Behaviours").Find("BordearFuego").GetComponent<droneAgent>().enabled = true;
                transform.Find("Behaviours").Find("BordearFuego").GetComponent<droneAgent>().actionToDirectMovement = false;
                transform.Find("Behaviours").Find("BordearFuego").GetComponent<droneAgent>().UseLocalOnEpisodeBegin = false;
                transform.Find("Behaviours").Find("BordearFuego").transform.GetComponent<BehaviorParameters>().Model = NeuralBordearFuego;
                transform.Find("Behaviours").Find("BordearFuego").transform.GetComponent<BehaviorParameters>().BehaviorType = BehaviorType.InferenceOnly;

                transform.Find("Behaviours").Find("IrBateriaManual").gameObject.SetActive(true);
                transform.Find("Behaviours").Find("IrBateriaManual").GetComponent<IrBateriaManual>().enabled = true;
                transform.Find("Behaviours").Find("IrBateriaManual").GetComponent<IrBateriaManual>().actionToDirectMovement = false;
                transform.Find("Behaviours").Find("IrBateriaManual").GetComponent<IrBateriaManual>().UseLocalOnEpisodeBegin = false;


                transform.Find("Behaviours").Find("EvitarObstaculo").gameObject.SetActive(true);
                transform.Find("Behaviours").Find("EvitarObstaculo").GetComponent<EvitarObstaculo>().enabled = true;
                transform.Find("Behaviours").Find("EvitarObstaculo").GetComponent<EvitarObstaculo>().UseLocalOnEpisodeBegin = false;
                transform.Find("Behaviours").Find("EvitarObstaculo").GetComponent<EvitarObstaculo>().actionToDirectMovement = false;
                transform.Find("Behaviours").Find("EvitarObstaculo").transform.GetComponent<BehaviorParameters>().Model = NeuralEsquivarObstaculo;
                transform.Find("Behaviours").Find("EvitarObstaculo").transform.GetComponent<BehaviorParameters>().BehaviorType = BehaviorType.InferenceOnly;

                transform.Find("Behaviours").transform.GetComponent<FusionModule>().gameObject.SetActive(true);
                break;
            case Tarea.BORDEARFUEGO:
                chargeStationParent.gameObject.SetActive(false);
                gridHeat.updateFireValue = true;

                transform.Find("Behaviours").GetComponent<DroneMovement>().bateryActive = false;
                transform.Find("Behaviours").Find("BordearFuego").gameObject.SetActive(true);
                transform.Find("Behaviours").Find("IrBateria").gameObject.SetActive(false);
                transform.Find("Behaviours").Find("EvitarObstaculo").gameObject.SetActive(false);
                transform.Find("Behaviours").Find("BordearFuego").GetComponent<droneAgent>().actionToDirectMovement = true;
                transform.Find("Behaviours").Find("BordearFuego").GetComponent<droneAgent>().UseLocalOnEpisodeBegin = true;
                transform.Find("Behaviours").Find("IrBateriaManual").GetComponent<IrBateriaManual>().actionToDirectMovement = false;
                transform.Find("Behaviours").Find("IrBateriaManual").GetComponent<IrBateriaManual>().UseLocalOnEpisodeBegin = false;
                //drone.transform.Find("Behaviours").Find("EvitarObstaculos").gameObject.SetActive(false);             

                transform.Find("Behaviours").transform.GetComponent<FusionModule>().gameObject.SetActive(false);
                break;

            case Tarea.BATERIA:
                chargeStationParent.gameObject.SetActive(true);
                gridHeat.updateFireValue = false; 
                
                transform.Find("Behaviours").GetComponent<DroneMovement>().bateryActive = true;
                transform.Find("Behaviours").Find("IrBateria").gameObject.SetActive(true);
                transform.Find("Behaviours").Find("BordearFuego").gameObject.SetActive(false);
                transform.Find("Behaviours").Find("EvitarObstaculo").gameObject.SetActive(false);
                //drone.transform.Find("Behaviours").Find("EvitarObstaculos").gameObject.SetActive(false);
                transform.Find("Behaviours").transform.GetComponent<FusionModule>().gameObject.SetActive(false);
                break;

            case Tarea.EVITAROBSTACULO:
                chargeStationParent.gameObject.SetActive(false);
                gridHeat.updateFireValue = false;

                transform.Find("Behaviours").GetComponent<DroneMovement>().bateryActive = false;
                transform.Find("Behaviours").Find("BordearFuego").gameObject.SetActive(false);
                transform.Find("Behaviours").Find("IrBateria").gameObject.SetActive(false);
                transform.Find("Behaviours").Find("EvitarObstaculo").gameObject.SetActive(true);
                transform.Find("Behaviours").Find("IrBateria").GetComponent<droneAgent>().actionToDirectMovement = true;
                transform.Find("Behaviours").Find("IrBateria").GetComponent<IrBateria>().UseLocalOnEpisodeBegin = true;
                transform.Find("Behaviours").Find("EvitarObstaculo").GetComponent<EvitarObstaculo>().UseLocalOnEpisodeBegin = true;
                
                transform.Find("Behaviours").transform.GetComponent<FusionModule>().gameObject.SetActive(false);
                break;
        }
    }  


    ///<sumary>
    /// Accion que se realiza unicamente al principio de cada episodio de entrenamiento.
    /// En este caso se colocara el dron o drones en una posicion valida y TODO: resetear el mapa
    ///</sumary>
    public override void OnEpisodeBegin(){
        DroneReadyToTrain = false;

        gridHeat.updateFireValue = true; 
        // reseteamos todos los elementos del campo de entrenamiento
        droneMovement.initiliazeBatery(70f);
        droneMovement.ResetRigidBody();
        gridHeat.resetGridHeat();
        moveChargeStationsToRandomPos();
        transform.Find("Behaviours").Find("IrBateriaManual").GetComponent<IrBateriaManual>().visitedStation = false;
        updateNearestChargeStation();
        UpdateNearestPerceivedFire();
        // reseteamos los sensores
        nearFire=true;
        SensorNearestFirePosition = gridHeat.getInitialFirePosition3D()+Vector3.up*20;
        SensornearestChargeStationPosition = chargeStationList[0].position+Vector3.up*20;
        SensordistanceToCharger = 999;
        
        droneMovement.initiliazeBatery(80f);
        nearestChargeStation = chargeStationList[0];
        updateNearestChargeStation();
        UpdateNearestPerceivedFire();

        DroneReadyToTrain = true;
    }



    ///<sumary>
    /// Una vez reciba la salida del la red neuronal lo tendra que enviar a cada behaviour
    /// este estimulo sera el encargado de determinar que comportamiento es el que va a destacar
    /// 0 : Bordear fuego
    /// 1 : Ir bateria
    /// 2 : Esquivar
    ///</sumary>
    public override void OnActionReceived(float[] vectorAction){
        behaviourActive = vectorAction[0];  //integer
        if(behaviourActive == 0){
            activity[0] = 1;
            activity[1] = 0;
            activity[2] = 0;
        }else if(behaviourActive == 1) {
            activity[0] = 0;
            activity[1] = 1;
            activity[2] = 0;
        }else{
            activity[0] = 0;
            activity[1] = 0;
            activity[2] = 1;
        }
        //Debug.Log("planning:" + activity[0] +" - "+ activity[1]+"- "+ activity[2]);
    }



    ///<sumary>
    /// Control Deterministico para comprobar el funcionamiento del planning
    ///</sumary>
    public override void Heuristic(float[] actionsOut){
        RayPerceptionInput spec = perceptionSensores.GetRayPerceptionInput();
    
        RayPerceptionOutput obs = RayPerceptionSensor.Perceive(spec);
        bool sensorActivado = obs.RayOutputs.Any( x => x.HitTaggedObject );
        float behaviourActiveAux;
        
        if(droneMovement.bateryValue < 30 || nearFire == false ){ // comprobamos la bateria
            //fuegoActivity = 0f;
            //bateriaActivity = 1f;
            //esquivarActivity = 0f; 
            behaviourActiveAux = 1;
        }else if(sensorActivado){ // Comprobamos si se le activa algun sensor
            behaviourActiveAux = 2;
        }else{  // sino que boordee fuego
            //fuegoActivity = 1f;
            //bateriaActivity = 0f;
            //esquivarActivity = 0f; 
            behaviourActiveAux = 0;
        }
        
        actionsOut[0] = behaviourActiveAux;
        //actionsOut[1] = bateriaActivity;
        //actionsOut[2] = esquivarActivity;
    }


    ///<sumary>
    /// Encargado de obtener todas las observaciones y enviarlas a cada una de los behaviours
    ///</sumary>
    public override void CollectObservations(VectorSensor sensor){
        // bordear juego
        sensor.AddObservation(transform.localPosition.x);
        sensor.AddObservation(transform.localPosition.y);

        //fire
        sensor.AddObservation(SensorNearestFirePosition - transform.position);
        sensor.AddObservation(SensorAngleWithFire);

        // bateria
        sensor.AddObservation(droneMovement.bateryValue / droneMovement.maxBatery);
        sensor.AddObservation(nearestChargeStation.position - transform.position);

        //esquivar son los sensores
    }


    // Esta funcion se encarga de introducir las estaciones en la lista para posteriormente
    // calcular los ensores de los drones
    public void findStation(){
        foreach(Transform station in chargeStationParent){
            chargeStationList.Add(station);
        }
    }

    // La funcion que comprueba los sensores
    private void FixedUpdate(){
        updateNearestChargeStation();
        updateAngleProyection();
        UpdateNearestPerceivedFire();
        droneMovement.SoftdrainBatery();
        CheckBateryNotEmpty();
        updateBeliveMap();
        GoingTofire();
        GoingToChargeStation();
        CheckIfOutoffMap();
    }

    // posicionamos las estaciones de carga de manera aleatoria por el borde del mapa
    private void moveChargeStationsToRandomPos(){
        int randX, randY;
        int gridSizeX = trainingTerrain.GetComponent<gridHeat>().getGridSizeX();
        int gridSizeY = trainingTerrain.GetComponent<gridHeat>().getGridSizeY();
        int randAux = UnityEngine.Random.Range(0, 1);
        for(int i = 0; i< chargeStationList.Count; i++){
            randAux = UnityEngine.Random.Range(0, 1);
            if(randAux == 0){
                randX = UnityEngine.Random.Range(0, 2) == 0 ? 4 : gridSizeX-4;
                randY = UnityEngine.Random.Range(4, gridSizeY-4);
            }else{
                randX = UnityEngine.Random.Range(4, gridSizeX-4);
                randY = UnityEngine.Random.Range(0, 2) == 0 ? 4: gridSizeY-4;
            }


            chargeStationList[i].transform.position = trainingTerrain.GetComponent<gridHeat>().WorldPointFromCell(new Vector2(randX, randY));
        }

        chargeStationList[0].transform.position = trainingTerrain.GetComponent<gridHeat>().WorldPointFromCell(new Vector2(3, 3));
        chargeStationList[1].transform.position = trainingTerrain.GetComponent<gridHeat>().WorldPointFromCell(new Vector2(46, 46));
    }

/*----------------------------------------------------------------------------------------
 *                                       SENSORES 
 *  
 *   En esta parte vamos a obtener los valores de los sensores para pasar a cada uno de lo
 *   Comportamientos.
 -----------------------------------------------------------------------------------------*/

    ///<sumary>
    /// Si se implementa lo de la percecion del fuego se puede usar para actualizar el ultimo fuego percibido
    ///</sumary>
    public void UpdateNearestPerceivedFire(){
        Vector2 indexPosition = gridHeat.CellFromWorldPoint(transform.position);
        Vector3 potentialNearestFirePosition;
        int x = (int)indexPosition.x;
        int y = (int)indexPosition.y;

        //Comprobamos las celdas que puede observar el dron
        for(int i=x-3; i<x+3; i++){
            for(int j=y-3; j<y+3  ; j++){
                if(gridHeat.isCellOnFire(i, j)){
                    potentialNearestFirePosition = gridHeat.getCellheat(i, j).worldPosition+Vector3.up*20;    //TODO: cambiar por altura de terrain , no 20
                    if(Vector3.Distance(transform.position, potentialNearestFirePosition) < Vector3.Distance(transform.position, SensorNearestFirePosition)){
                        SensorNearestFirePosition = potentialNearestFirePosition;
                        
                    }
                }
            }
        }
        
        return;
    }


    ///<sumary>
    // Comprueba cual es la estacion de carga mas cercana que tiene y la actualiza apra el sensor
    ///</sumary>
    private void updateNearestChargeStation(){
        Vector3 potentialStationPosition;

        foreach(Transform station in chargeStationList){
            potentialStationPosition = station.transform.position+Vector3.up*20;
            if(Vector3.Distance(transform.position, potentialStationPosition) < Vector3.Distance(transform.position, SensornearestChargeStationPosition)){
                nearestChargeStation = station;

                SensornearestChargeStationPosition = nearestChargeStation.transform.position + Vector3.up*20;
            }
        }
    }


    ///<sumary>
    /// Funcion que devuelve el angulo entre el vector velocidad y la posicion proyectada 
    /// en el plano donde se situa el dron.
    ///</sumary>
    private float getAngleProyection(Vector3 pos){
        float angle;
        Vector3 vectorProyection;
        vectorProyection = new Vector3(pos.x, transform.position.y , pos.z) - transform.position ;
        //Debug.DrawRay(transform.position, vectorProyection, Color.green);
        angle = Vector3.SignedAngle(-transform.forward, vectorProyection, Vector3.up);

        angle = (angle + 180 % 360);
        return angle;
    }






/*----------------------------------------------------------------------------------------
 *                                      REWARDS
 *
 *  Para poder entrenar el planing tenemos que obtener el reward de cada uno de los 
 *  comportamientos con el objetivo que el planing pueda aprender a indicar qeu behaviour es 
 *  el mas adecuado para cada momento.
 -----------------------------------------------------------------------------------------*/

    ///<sumary>
    /// Comprueba en que celda esta, si no esta en ninguna celda se encuentra fuera de
    /// terreno. Usamos el velctor -999 como vector null.
    ///</sumary>
    private void CheckIfOutoffMap(){
        Vector2 indexPosition = trainingTerrain.GetComponent<gridHeat>().CellFromWorldPoint(transform.position);
        if(indexPosition.x <= -999){
            AddReward(-10f);
            EndEpisode();
        }
        return;
    }

    
    ///<sumary>
    // Comprueba si se queda sin bateria, si es así le penaliza y acaba el episodio
    ///</sumary>
    private void CheckBateryNotEmpty(){
        if(droneMovement.bateryValue <= 0){
            AddReward(-10f);
            EndEpisode();
        }
    }


     ///<sumary>
    // Le fomentamos cuando se dirije hacia la estacion de carga con poca carga
    ///</sumary>
    private void GoingToChargeStation(){
        float actualDistanceToStation = Vector3.Distance(transform.position, SensornearestChargeStationPosition);
        float distanceDiffToStation = SensordistanceToCharger - actualDistanceToStation;

        if(droneMovement.bateryValue < 30){
            nearFire = false;
            if(distanceDiffToStation > 4 ){
                AddReward(1f);
                SensordistanceToCharger = actualDistanceToStation;
            }
        }
    }

    ///<sumary>
    // Le fomentamos cuando se dirije hacia el fuego con mucha carga
    ///</sumary>
    private void GoingTofire(){
        float actualDistanceToFire = Vector3.Distance(transform.position, SensorNearestFirePosition);
        float distanceDiff = distanceToFire - actualDistanceToFire;

        if(distanceDiff > 4 ){
            distanceToFire = actualDistanceToFire;
            AddReward(1f);
        }
        if(actualDistanceToFire <= 2 && droneMovement.bateryValue > 80){
            AddReward(5f);
            nearFire = true;
            //EndEpisode();
        } 

    }

    ///<sumary>
    // Fucion qeu comprueba la distacia con el ultimo fuego detectado
    // y otorga un reward acorde
    ///</sumary>
    void CheckIfNearFire(){
        Vector3 proyectedPos = new Vector3(SensorNearestFirePosition.x, transform.position.y, SensorNearestFirePosition.z);
        if(Vector3.Distance(transform.position, proyectedPos) < 12){
            AddReward(0.001f);
        }
    }


    ///<sumary>
    // Funcion que comprueba si el angulo con el fuego va aumentando o reduciendo 
    // para fomentar que los drones puedan moverse en sentido horario
    ///</sumary>
    void updateAngleProyection(){
        angle = getAngleProyection(SensorNearestFirePosition);
        if(angle > SensorAngleWithFire && (angle-SensorAngleWithFire > 0.25))AddReward(0.2f);
            
        SensorAngleWithFire = angle;
    }


    
    ///<sumary>
    /// Funcion que comprueba el estado de la zona que sobrevuela y lo actualiza en 
    /// en mapa.
    ///</sumary>
    void updateBeliveMap(){
        Vector2 indexPosition = gridHeat.CellFromWorldPoint(transform.position);
        int x = (int)indexPosition.x;
        int y = (int)indexPosition.y;
        int sizeX = gridHeat.getGridSizeX();
        int sizeY = gridHeat.getGridSizeY();
        for(int i=x-3; i<x+3 && i > 0 && i < sizeX; i++){
            for(int j=y-3; j<y+3 && j > 0 && j < sizeY; j++){

                if(beliveMap.values[i,j] == Status.Normal && gridHeat.cells[i, j].status == Status.OnFire){ //Reward si descubre un cambio de juego
                    AddReward(1);
                }
                beliveMap.UpdateCellMap(i, j, gridHeat.cells[i,j].status);
            }
        }
        beliveMap.updateUV();
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
            //AddReward(-.2f);
        }else if(other.CompareTag("charger")){
            if(nearFire == false){
                
                droneMovement.charging();
                charging = true;

                if(droneMovement.bateryValue >= 99 ){
         
                    charging = false;
                    distanceToFire = Vector3.Distance(transform.position, SensorNearestFirePosition);
                }

            }
        }
    }
    



}
