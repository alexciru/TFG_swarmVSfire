using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using System;


///<sumary>
/// Script agente que interactua con mlAgent y conececta con otros scripts
/// actualmente se encarga del behaviour de 
///</sumary>
public class droneAgent : Agent {

    // Variables GLobales del sistema  
    public GameObject trainingTerrain;
    private gridHeat gridHeat;
    private DroneMovement droneMovement;
    private FusionModule fusionModule;
    private planningModule planningModule;

    private BeliveMap beliveMap;
    // vector hacia el ultimo fuego encontrado
    private Vector3 nearestFirePosition;
    private Vector2 ultimaCelda;
    private float angleWithFire;
    private float angle;
    public bool DroneReadyToTrain;

    /// For training this behaviour
    public bool actionToDirectMovement = true;
    public bool UseLocalOnEpisodeBegin = true;
    public float activity; //Valor entre 0 y 1 para el IB2C

    // To check distance with other vehicules
    public List<Transform> partnerDrones;
    Transform clossestDrone; 
    
    ///<sumary>
    /// Inicializa el agente al empezar el entrenamiento
    /// Es utilizada para obtener los componentes del drone
    ///</sumary>
    public override void Initialize(){

        if(trainingTerrain == null) print("ERROR: Campo de entrenamiento no asignado al agente");
        Debug.Log("Bordear Fuego Inicializado");
        droneMovement = transform.parent.GetComponent<DroneMovement>();
        fusionModule = transform.parent.GetComponent<FusionModule>();
        planningModule = transform.parent.parent.GetComponent<planningModule>();
        gridHeat = trainingTerrain.GetComponent<gridHeat>();
        
        activity = 1;
        //Como el believ map se encuentra compartido lo obtenemos de GridHeat
        beliveMap = transform.parent.parent.parent.Find("BeliveMap").GetComponent<BeliveMap>();
        //findDronesForTraining();
        clossestDrone = partnerDrones[0];
    }

    ///<sumary>
    /// Accion que se realiza unicamente al principio de cada episodio de entrenamiento.
    /// En este caso se colocara el dron o drones en una posicion valida y TODO: resetear el mapa
    ///</sumary>
    public override void OnEpisodeBegin(){
        if(UseLocalOnEpisodeBegin){
            DroneReadyToTrain = false;
            droneMovement.bateryActive = false;
            nearestFirePosition = Vector3.zero;
            beliveMap.InizializeMap();
            droneMovement.ResetRigidBody();
            gridHeat.resetGridHeat();
            DroneReadyToTrain = true;
            activity = 1;
        }
    }

    ///<sumary>
    /// Se llama cuando se recibe una instruccio o del jugador o de la red neuronal.
    /// para el drone solo podra moverse en el espacio 2D y rotar.
    /// index 0: move vector x  -> (+1 = right, -1 = left)
    /// index 1: move vector z  -> (+1 forward, -1 = backward)
    ///</sumary>
    public override void OnActionReceived(float[] vectorAction){
        //if(!UseLocalOnEpisodeBegin) activationFuntion();
        //if(activity == 1){
            Vector3 move = new Vector3(vectorAction[0], 0, vectorAction[1]);
            if(actionToDirectMovement){
                //Debug.Log("Se activa en UpdateMovement");
                droneMovement.UpdateMovement(move);
            }else{
                fusionModule.velocities[0] = move; 
                fusionModule.activities[0] = activity;
            }
        //}
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

        if(UseLocalOnEpisodeBegin){
            Vector3 aux = nearestFirePosition - transform.position;
            sensor.AddObservation(aux.normalized);
            sensor.AddObservation(Vector3.Distance(nearestFirePosition, transform.position));
            sensor.AddObservation(angleWithFire); 
            //vector velocidad
        }else{
            Vector3 aux = planningModule.SensorNearestFirePosition - transform.position;
            sensor.AddObservation(aux.normalized);
            sensor.AddObservation(Vector3.Distance(planningModule.SensorNearestFirePosition, transform.position));
            sensor.AddObservation(planningModule.SensorAngleWithFire);
       
        }
        // posicion en el mapa 
        sensor.AddObservation(clossestDrone.transform.position.x);
        sensor.AddObservation(clossestDrone.transform.position.y);
    }
    
    // se llama cada frame
    // se puede usar para debugear, pintando una linea con la celda mas cercana donde ha encontrado fuego
    private void FixedUpdate(){
        
        if(DroneReadyToTrain && UseLocalOnEpisodeBegin){
            AddReward(-.001f);
            UpdateNearestPerceivedFire();
            Debug.DrawLine(transform.position, nearestFirePosition, Color.red);
            CheckIfOutoffMap();
            updateAngleProyection();
            updateBeliveMap();
            CheckIfNearFire();
            checkDsitanceToOtherDrones();
        }
        //UpdateClossestDrone();
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
    private void OnTriggerEnter(Collider other){
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
            Debug.Log("Penalizado");
            AddReward(-.2f);
        }
    }
 
 /*----------------------------------------------------------------------------------------
 *                                       SENSORES 
 *  
 *   En esta parte vamos a obtener los valores de los sensores para pasar a cada uno de lo
 *   Comportamientos.
 *----------------------------------------------------------------------------------------*/
    

    ///<sumary>
    /// Si se implementa lo de la percecion del fuego se puede usar para actualizar el ultimo fuego percibido
    /// TODO: esto seguramente se pueda hacer mas bonito
    ///</sumary>
    public void UpdateNearestPerceivedFire(){
        Vector2 indexPosition = gridHeat.CellFromWorldPoint(transform.position);
        Vector3 potentialNearestFirePosition;
        int x = (int)indexPosition.x;
        int y = (int)indexPosition.y;

        //TODO: Comprobamos las celdas que puede observar el dron
        for(int i=x-3; i<x+3; i++){
            for(int j=y-3; j<y+3  ; j++){
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
    /// Funcion que añade como sensor el estado de las celdas 
    /// Del cual nos encontramos encima nuestra.
    /// vector -999 como si fuese null
    /// simularia lo qeu captaria una camara termica en el drone.
    ///</sumary>
    private void AddCellStatesUnderDrone(VectorSensor sensores){
        Vector2 indexPosition = gridHeat.CellFromWorldPoint(transform.position);
        if(indexPosition.x <= -999){return;} 
        int gridSizeX = gridHeat.getWorldSizeX();
        int gridSizeY = gridHeat.getWorldSizeY();

        int x = (int)indexPosition.x;
        int y = (int)indexPosition.y;
        //Comprobamos las celdas que puede observar el dron
        for(int i=x-2; i<x+2; i++){
            for(int j=y-2; j<y+2  ; j++){
                sensores.AddObservation(gridHeat.isCellOnFire(i, j));
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
 *   
 *----------------------------------------------------------------------------------------*/

    ///<sumary>
    /// Comprueba en que celda esta, si no esta en ninguna celda se encuentra fuera de
    /// terreno. Usamos el velctor -999 como vector null.
    ///</sumary>
    private void CheckIfOutoffMap(){
        Vector2 indexPosition = trainingTerrain.GetComponent<gridHeat>().CellFromWorldPoint(transform.position);
        if(indexPosition.x <= -999){
            AddReward(-300f);
            EndEpisode();
        }
        return;
    }


    //comprueba si se esta moviendo entre celdas 
    void ComprobarCambioCelda(){
        Vector2 pos = trainingTerrain.GetComponent<gridHeat>().WorldPointFromCell(transform.position);
        if(ultimaCelda != pos){
            ultimaCelda = pos;
            AddReward(0.05f);
        }
    }  


    // Fucion qeu comprueba la distacia con el ultimo fuego detectado
    // y otorga un reward acorde
    void CheckIfNearFire(){
        Vector3 proyectedPos = new Vector3(nearestFirePosition.x, transform.position.y, nearestFirePosition.z);
        float distance = Vector3.Distance(transform.position, proyectedPos);
        if(distance < 12 ){
            AddReward(0.001f);
        }
        if(distance < 2){
            AddReward(-0.003f);
        }
    }


    ///<sumary>
    /// Funcion que comprueba si el angulo con el fuego va aumentando o reduciendo 
    /// para fomentar que los drones puedan moverse en sentido horario
    ///</sumary>
    void updateAngleProyection(){
        angle = getAngleProyection(nearestFirePosition);
        if(angle > angleWithFire && (angle-angleWithFire > 0.25)){
            AddReward(0.2f);
        }
            
        angleWithFire = angle;
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



    void checkDsitanceToOtherDrones(){
        foreach(Transform drone in partnerDrones){
            if(Vector3.Distance(transform.position, drone.transform.position) <  10){
                AddReward(-0.5f);
            }
        }
    }



    void UpdateClossestDrone(){
        foreach(Transform drone in partnerDrones){
            if(Vector3.Distance(transform.position, drone.position) > 1 ){
                if(Vector3.Distance(transform.position, drone.position) < Vector3.Distance(transform.position, clossestDrone.position)){
                    clossestDrone = drone;
                }
                
            }
        }
    }


      // Obtiene los diferentes drones dentro del campo de entenamiento
    public void findDronesForTraining(){
        partnerDrones = new List<Transform>();
        for(int i=0; i< transform.parent.parent.parent.transform.childCount ;i++){
            Transform child = transform.parent.parent.parent.transform.GetChild(i);
            if(child.CompareTag("drone")){
                partnerDrones.Add(child);
            }
        }
    }


}


