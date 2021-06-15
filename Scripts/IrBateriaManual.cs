using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IrBateriaManual : MonoBehaviour
{
    public GameObject trainingTerrain;
    private gridHeat gridHeat;
    private DroneMovement droneMovement;
    private FusionModule  fusionModule;

    private planningModule planningModule;
    private Transform nearestChargeStation;
    private Vector3 nearestChargeStationPosition;
    private Vector3 nearestFirePosition;
    private float distanceToFire;
    private float distanceToCharger;

    private bool isCharging;
    // boleanos de control de acciones
    // private bool isCharging = false;
    public bool visitedStation=false;
    //private bool DroneReadyToTrain = false;
    private float activity;
    // Control actions with or without planning
    public bool actionToDirectMovement = true;
    public bool UseLocalOnEpisodeBegin = true;
    public bool useHeuristics = false;

private List<Transform> chargeStationList;



    // Funcion que la utilizamos para inicializar todos los componentes
    // y para declara las variables necesarias que vamos a necesitar
    void Start()
    {
        if(trainingTerrain == null) print("ERROR: Campo de entrenamiento no asignado al agente");
        Debug.Log("Bateria Manual Inicializado");
        droneMovement = transform.parent.transform.GetComponent<DroneMovement>();
        gridHeat = trainingTerrain.GetComponent<gridHeat>();
        fusionModule = transform.parent.GetComponent<FusionModule>();
        chargeStationList = gridHeat.findChargeStation();
        planningModule = transform.parent.parent.GetComponent<planningModule>();
        distanceToCharger = 999;
        OnEpisodeBegin();
    }



    ///<sumary>
    /// Accion que se realiza unicamente al principio de cada episodio de entrenamiento.
    /// En este caso se colocara el dron o drones en una posicion valida y TODO: resetear el mapa
    ///</sumary>
    public void OnEpisodeBegin(){
        if(UseLocalOnEpisodeBegin){
            gridHeat.updateFireValue = false; //nidicamos que no queremos actualizar el incendio
            //DroneReadyToTrain = false; //control para no activar rewards si no esta colocado los drones        
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
            //isCharging = false;
            activity = 1;
            visitedStation=false;
            //DroneReadyToTrain = true; //control para no activar rewards si no esta colocado los drones        
        }
    }

    // Funcion llamada por el modulo planing para dada unos valores de los sensores tomar una accion
    // El funcionamiento es:
    //   Si el dron se  encuentra con una bateria inferior a 30 se dirige hacia la estacion de carga
    //   Si se encuentra en la estacion de carga con carga completa se dirige hacia el fuego 
    //   TODO: aumentar la logica para la interaccion entre drones
    //   
    //   Salida:  vector accion [0, 0, 0] donde es [x,y - freno]
    float[] TakeAction()
    {
        float[] vectorAction = new float[3];

        //if(isCharging) return new float[] {0,0,0}; // si esta cargando lo dejamos parado
        float actualDistanceToCharger = Vector3.Distance(transform.position, nearestChargeStationPosition);
        if(droneMovement.bateryValue < 30 || isCharging){ //nos dirigimos a la estacion de carga
            Vector3 chargerDirection = (nearestChargeStationPosition - transform.position).normalized;
            
            vectorAction[0] = chargerDirection.x;
            vectorAction[1] = chargerDirection.z;
            vectorAction[2] = 0f;
            
        }else if(visitedStation){ // si ya ha recargado que vuelva al fuego
            Vector3 FireDirection = (nearestFirePosition - transform.position).normalized;
            vectorAction[0] = FireDirection.x;
            vectorAction[1] = FireDirection.z;
            vectorAction[2] = 0f;
           
        }else{      //si esta cerca del fuego 
            vectorAction[0] = 0f;
            vectorAction[1] = 0f;
            vectorAction[2] = 0f;
        }

        return vectorAction;
    }


    // Funcion que se llama cada frame la usarlemos apra actualizar los 
    // sensores en cada frama
    void FixedUpdate()
    {         
        //nearestFirePosition = gridHeat.getInitialFirePosition3D()+Vector3.up*20;
        droneMovement.SoftdrainBatery();
        updateNearestChargeStation();
        GoingToChargeStation();
        OnActionReceived(TakeAction()); // llamamos a que haga accion
        Debug.DrawLine(transform.position, nearestFirePosition, Color.red);
        Debug.DrawLine(transform.position, nearestChargeStationPosition, Color.green);
        nearestFirePosition = planningModule.SensorNearestFirePosition;
        CheckIfNearFire();
    }


   // actualiza la funcion d activcion del iB2C
    private void activationFuntion(){
        activity = planningModule.stimulous[0]; 
    }

    ///<sumary>
    /// Se llama cuando se recibe una instruccio o del jugador o de la red neuronal.
    /// para el drone solo podra moverse en el espacio 2D y rotar.
    /// index 0: move vector x  -> (+1 = right, -1 = left)
    /// index 1: move vector z  -> (+1 forward, -1 = backward)
    /// index 2: rotation (yaw) -> (-1 left  --- +1 right)
    ///</sumary>
    public void OnActionReceived(float[] vectorAction){
        Vector3 move = new Vector3(vectorAction[0], 0, vectorAction[1]);

        if(vectorAction[2] < 0f){
            move = Vector3.zero;  // activamos para quie se quede quieto
        }

        if(actionToDirectMovement){
            droneMovement.UpdateMovement(move);
        }else{
            fusionModule.velocities[1] = move; 
            fusionModule.activities[1] = activity;
        }
        
    }

    


    // Fucion qeu comprueba la distacia con el ultimo fuego detectado
    // y otorga un reward acorde
    void CheckIfNearFire(){
        //Vector3 proyectedPos = new Vector3(nearestFirePosition.x, transform.position.y, nearestFirePosition.z);
        if(Vector3.Distance(transform.position, nearestFirePosition) < 4){
            visitedStation = false;
        }

    }



    ///<sumary>
    /// Si no hay bateria repetimos el episodio
    /// Esta funcion no tiene mucha utilidad, hay que poner modo heuristic
    ///</sumary>
    private void CheckBateryNotEmpty(){
        if(droneMovement.bateryValue <= 0){
            OnEpisodeBegin();
        }
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




    // posicionamos las estaciones de carga de manera aleatoria por el borde del mapa
    // En caso de usar curricullum vamos a ir incrementntandolo con el tiempo "chargerDistance"
    // 
    private void moveChargeStationsToRandomPos(){
        int randX, randY;
        int gridSizeX = trainingTerrain.GetComponent<gridHeat>().getGridSizeX();
        int gridSizeY = trainingTerrain.GetComponent<gridHeat>().getGridSizeY();
        int distanceCharger = 2;
        
        
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

    // ------------------------------------------------------------------------------------------------------------------------
    // -------------------------------------------       COLLISION TRIGGUERS            -----------------------------------
    // ------------------------------------------------------------------------------------------------------------------------

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
    /// Funcion propia para contar el contacto con el dron. Se se encuentra en contacto con
    /// la estacion de carga indicamos quee esta cargando.
    ///</sumary>
    private void TriggerEnterOrStay(Collider other){
        if(other.CompareTag("charger")){
            if(visitedStation == false){
                droneMovement.charging();
                isCharging = true;
                if(droneMovement.bateryValue >= 98){
                    isCharging = false;
                    distanceToFire = Vector3.Distance(transform.position, nearestFirePosition);
                    visitedStation = true;
                }

            }
        }
    }




        ///<sumary>
    /// Le fomentamos cuando se dirije hacia la estacion de carga con poca carga
    ///</sumary>
    private void GoingToChargeStation(){
        float actualDistanceToStation = Vector3.Distance(transform.position, nearestChargeStationPosition);
        float distanceDiffToStation = distanceToCharger - actualDistanceToStation;
        if(distanceDiffToStation > 2 && visitedStation == false){
            distanceToCharger = actualDistanceToStation;
        }
    }


}
