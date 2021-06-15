// using System.Collections.Generic;
// using UnityEngine;
// using Unity.MLAgents;
// using Unity.MLAgents.Sensors;
// using System;

// public class droneAgent : Agent {
//     [Tooltip("Fuerza que se aplica para mover")]
//     public float moveForce = 2f;

//     [Tooltip("velocidad de rotar respecto al eje")]
//     public float yawSpeed = 100f;

//     [Tooltip("La camara del agente")]
//     public Camera agentCamera;

//     [Tooltip("Si esta entrenando o no")]
//     public bool trainingMode;

//     // rigidbody del agente (se pone el new para quitar un depretated)
//     new private Rigidbody rigidbody;
//     public GameObject trainingTerrain;

//     // vector hacia el ultimo fuego encontrado
//     private Vector3 nearesFirePosition;
//     private Vector3 nearestFireVectorProyection;

//     // El terreno donde esta entrenando
//     private Terrain TrainingField;

//     // Permite para un movimineto mas sueave
//     private float smoothYawChange = 0f;
//     Vector2 ultimaCelda;

//     // si el agente esta bloquado (no esta volando porque no quiere)
//     private bool frozen = false;
//     Vector3 worldBottomLeft;
//     float angleWithFire;
//     float angle;
//     bool DroneReadyToTrain;

//     ///<sumary>
//     /// Inicializa el agente
//     ///</sumary>
//     public override void Initialize(){
//         rigidbody = GetComponent<Rigidbody>();
//         if(trainingTerrain == null) print("ERROR: Campo de entrenamiento no asignado al agente");
//         worldBottomLeft = trainingTerrain.GetComponent<gridHeat>().getWorldBottomLeft();
//         // si no esta entrenando ->  no para de actuar
//         //if(!trainingMode) MaxStep = 0;
//     }

//     ///<sumary>
//     /// Accion que se realiza unicamente al principio de cada episodio de entrenamiento.
//     /// En este caso se colocara el dron o drones en una posicion valida y TODO: resetear el mapa
//     ///</sumary>
//     public override void OnEpisodeBegin(){
//         // ponemos a 0 las velocidades al inicio de cada episodio
//         DroneReadyToTrain = false;
//         rigidbody.velocity = Vector3.zero;
//         rigidbody.angularVelocity = Vector3.zero;

//         bool inFrontOfFire = true;  //TODO: spawnea con 50% enfrente del fuego durante entrenamiento
//         if(trainingMode){
//             inFrontOfFire = true;
//         }
//         nearesFirePosition = Vector3.zero;
//         trainingTerrain.GetComponent<gridHeat>().resetGridHeat();
//     }

//     ///<sumary>
//     /// Se llama cuando se recibe una instruccio o del jugador o de la red neuronal.
//     /// para el drone solo podra moverse en el espacio 2D y rotar.
//     /// index 0: move vector x  -> (+1 = right, -1 = left)
//     /// index 1: move vector z  -> (+1 forward, -1 = backward)
//     /// index 2: rotation (yaw) -> (-1 left  --- +1 right)
//     ///</sumary>
//     public override void OnActionReceived(float[] vectorAction){
//         if(frozen) return; // si esta congelado no tomar accion
//         if(vectorAction.Length == 0){
//             print("vector accion a 0");
//             return;
//         } 
//         // Vector velocidad: calcula vector de movimiento y aplicamos
//         Vector3 move = new Vector3(vectorAction[0], 0, vectorAction[1]);
//         rigidbody.AddForce(move*moveForce);

//         /*
//         //obtemos rotacion y le aplicamos la rotacion suave
//         Vector3 rotationVector = transform.rotation.eulerAngles;
//         float yawChange = vectorAction[2];
//         // calculamos los cambios suaves
//         smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f*Time.deltaTime);
//         float yaw = rotationVector.y + smoothYawChange * Time.deltaTime * yawSpeed;
//         transform.rotation = Quaternion.Euler(0f, yaw, 0f);
//         */
//     }

//     ///<sumary>
//     /// Collect vector observation from the envioroment
//     /// Posibles sensores:
//     ///</sumary>
//     public override void CollectObservations(VectorSensor sensor){
//         // posicion en el mapa 
//         sensor.AddObservation(transform.localPosition.x);
//         sensor.AddObservation(transform.localPosition.y);

//         //rotacion actual
//         //sensor.AddObservation(transform.localRotation.normalized);

//         //producto escalar entre el dron y el fuego ¿sufienciente? no tiene pinta
//         //TODO: poner mejor el angulo ? para ver si apunta hacia el fuego indica si esta detraws o delante ?

//         // vector proyectado que apunta a la posicion del ultimo fuego avistado
//         sensor.AddObservation(nearestFireVectorProyection);

//         //vector velocidad
//         //sensor.AddObservation(rigidbody.velocity.normalized);
//         sensor.AddObservation(angleWithFire);
//         // TODO: Añadir posicion del compañero
//         // sensor.AddObservation(compañeroDron.transform.localPosition)


//         // TODO: añadir estado celdas debajo del drone
//         //AddCellStatesUnderDrone(VectorSensor sensor);
//     }

//     ///<sumary>
//     /// Cuando se pone el behaviou en "heuristics only" esta funcion sera llamada para que
//     /// que el jugador tome el control. La salida es el array de acciones en actionsOut.
//     /// <see cref="OnActionRecived(float[])"/> instead of using neural network
//     ///</sumary>
//     public override void Heuristic(float[] actionsOut){
//         Vector3 forward = Vector3.zero;
//         Vector3 left = Vector3.zero;
//         float yaw = 0f;

//         // convierte teclas en movimientos. solo o 1 o 0
//         if(Input.GetKey(KeyCode.W)) forward = transform.forward;
//         else if(Input.GetKey(KeyCode.S)) forward = -transform.forward;

//         if(Input.GetKey(KeyCode.A)) left = -transform.right;
//         else if(Input.GetKey(KeyCode.D)) left = transform.right;

//         /*
//         if(Input.GetKey(KeyCode.Q)) yaw = -1f;
//         else if(Input.GetKey(KeyCode.E)) yaw = 1f;
//         */
//         Vector3 combined = (forward + left).normalized;
//         actionsOut[0] = combined.x;
//         actionsOut[1] = combined.z;
//         //actionsOut[2] = yaw;
//     }

//     ///<sumary>
//     /// Coloca al eagente en una posicion valida. Se llamara al inico de cada episodio.
//     /// comprueba que no esta muy cerca del otro dron.
//     ///</sumary>
//     public void MoveToRandomPosition(Vector2 initialFirePosition){

//         transform.position = worldBottomLeft; // les movemos a la esquina de abajo antes de colorcarlo

//         bool safePositionFound = false;
//         if(trainingTerrain == null) print("ERROR: Terreno de entrenamiento no asignado en move position");
//         if(trainingTerrain == null) return;
//         bool inFrontOfFire = true; //TODO: cambiar esto en un futuro

//         // Obtenemos posicion del campo de entrenamiento
//         Quaternion potentialRotation;
//         Vector3 potentialPosition = Vector3.zero;
//         int intentos = 200;
//         int randX = 0;
//         int randY = 0;
//         Vector2 potentialCell;
//         float sizeX = trainingTerrain.GetComponent<gridHeat>().getWorldSizeX();
//         float sizeY = trainingTerrain.GetComponent<gridHeat>().getWorldSizeY();
//         float cellDiameter = trainingTerrain.GetComponent<gridHeat>().getCellDiameter();
//         while( !safePositionFound && intentos>0){
//             if(inFrontOfFire){
//                 int angle = UnityEngine.Random.Range(0,360);
                
//                 randX = (int)Math.Round( initialFirePosition.x + (3.5 * Math.Cos( angle ) ));
//                 randY = (int)Math.Round( initialFirePosition.y + (3.5 * Math.Sin( angle ) ));
                
//                 potentialCell = new Vector2(randX, randY);
    
//                 potentialPosition = trainingTerrain.GetComponent<gridHeat>().WorldPointFromCell(potentialCell);
//                 potentialPosition.y = 20;
    
//             }else{
//                 //posicion aleatoria dentro del margen (0,0 es esquina izquierda)
//                 randX = (int)UnityEngine.Random.Range(1, sizeX-1);
//                 randY = (int)UnityEngine.Random.Range(1, sizeY-1);
//                 potentialCell = new Vector2(randX, randY);
//                 potentialPosition = trainingTerrain.GetComponent<gridHeat>().WorldPointFromCell(potentialCell);
//                 potentialPosition.y = 20;
//             }
//             intentos--;
//             float yaw = UnityEngine.Random.Range(-180f, 180f);
//             potentialRotation = Quaternion.Euler(0f, yaw, 0f);

//             //comprobamos colisiones
//             Collider[] colliders = Physics.OverlapSphere(potentialPosition, cellDiameter*2);

//             if(colliders.Length == 0){
//                 safePositionFound = true;
//             }else{
//                 intentos--;
//                 if(intentos == 0) print("ERROR: no se encontro posicion valida para dron");
//             }
            
//         }
//         transform.position = potentialPosition;
//         DroneReadyToTrain = true; //control para no activar rewards si no esta colocado los drones
//     }


 
//     /*-------------------------COLLIDERS Y TRIGUERS ------------------------------*/

//     ///<sumary>
//     /// Funcion que se activa la primera vez qeu hay colision de colliders
//     ///</sumary>
//     private void OnTriggerEnter(Collider other){
//         TriggerEnterOrStay(other);
//     }

//     ///<sumary>
//     /// Funcion que se activa cuando se encuentra el collider dentro de otro
//     ///</sumary>
//     private void OnTriggerStay(Collider other){
//         TriggerEnterOrStay(other);
//     }

//     ///<sumary>
//     /// Funcion propia para contar el contacto con el dron
//     ///</sumary>
//     private void TriggerEnterOrStay(Collider other){
//         if(trainingMode && other.CompareTag("drone")){
//             AddReward(-.2f);
//         }
//     }

//     ///<sumary>
//     /// Se activa cuando se choca el collider con un objeto con otro collider.
//     /// Lo usamos para comprobar si se ha salido del mapa
//     /// TODO: esto no se activa, solucoinar en un futuro ? o no ?
//     ///</sumary>
//     private void OnCollisionEnter(Collision collision){
//        /* if(trainingMode && collision.collider.CompareTag("frontera")){
//             //print("Frontera detectada");
//             AddReward(-.5f);
//         }
//         */
//     }

//     /*--------------------------------- POSIBLES SENSORES Y REWARDS --------------------------------*/
    
        
//     // se llama cada frame
//     // se puede usar para debugear, pintando una linea con la celda mas cercana donde ha encontrado fuego
//     private void FixedUpdate(){
//         if(DroneReadyToTrain){
//             AddReward(-.001f);
//             UpdateNearestPerceivedFire();
//             Debug.DrawLine(transform.position, nearesFirePosition, Color.red);
//             CheckIfOutoffMap();
//             angle = getAngleProyection(nearesFirePosition);
//             //TODO: comprobar threshold
//             if(angle > angleWithFire && (angle-angleWithFire > 0.25)){
//                 AddReward(0.3f);
//             }
//             angleWithFire = angle;
//             toFarFromFire();
//         }
        
//     }


//     // Se llama en cada medio frame
//     // en el tutorial lo utilizaba para actualizar el fuego mas cercano
//     /*private void FixedUpdate(){
//         UpdateNearestPerceivedFire();
//     }*/


//     ///<sumary>
//     /// Si se implementa lo de la percecion del fuego se puede usar para actualizar el ultimo fuego percibido
//     /// TODO: esto seguramente se pueda hacer mas bonito
//     ///</sumary>
//     public void UpdateNearestPerceivedFire(){
//         // obtenemos la celda del drone
//         Vector2 indexPosition = trainingTerrain.GetComponent<gridHeat>().CellFromWorldPoint(transform.position);
//         int gridSizeX = trainingTerrain.GetComponent<gridHeat>().getWorldSizeX();
//         int gridSizeY = trainingTerrain.GetComponent<gridHeat>().getWorldSizeY();
//         Vector3 potentialNearestFirePosition;
//         List<Vector3> firePositions = new List<Vector3>();
//         int x = (int)indexPosition.x;
//         int y = (int)indexPosition.y;


//         //Comprobamos las celdas que puede observar el dron
//         for(int i=x-3; i<x+3; i++){
//             for(int j=y-3; j<y+3  ; j++){
//                 if(trainingTerrain.GetComponent<gridHeat>().isCellOnFire(i, j)){
//                     firePositions.Add(trainingTerrain.GetComponent<gridHeat>().getCellheat(i, j).worldPosition);
//                 }
//             }
//         }
//         //Actualizamos la celda mas cercana si la hay
//         if(firePositions.Count == 0) return;
//         potentialNearestFirePosition = firePositions[0]; //asignamos el primer valor para evitar errores
//         foreach(Vector3 pos in firePositions){
//             if(Vector3.Distance(transform.position, potentialNearestFirePosition) > Vector3.Distance(transform.position, pos)){
//                 potentialNearestFirePosition = pos;
//             }
//         }
//         if(Vector3.Distance(transform.position, potentialNearestFirePosition) < Vector3.Distance(transform.position, nearesFirePosition)){
//             nearesFirePosition = potentialNearestFirePosition;
//             AddReward(.1f);
//         }

//         nearestFireVectorProyection = new Vector3(nearesFirePosition.x, transform.position.y , nearesFirePosition.z) - transform.position ; 
//         return;
//     }



//     ///<sumary>
//     /// Funcion que añade como sensor el estado de las celdas 
//     /// Del cual nos encontramos encima nuestra.
//     /// vector -999 como si fuese null
//     /// simularia lo qeu captaria una camara termica en el drone.
//     ///</sumary>
//     private void AddCellStatesUnderDrone(VectorSensor sensor){
//         Vector2 indexPosition = trainingTerrain.GetComponent<gridHeat>().CellFromWorldPoint(transform.position);
//         print(indexPosition.x);
//         if(indexPosition.x <= -999){
//             return;
//         } 
//         int gridSizeX = trainingTerrain.GetComponent<gridHeat>().getWorldSizeX();
//         int gridSizeY = trainingTerrain.GetComponent<gridHeat>().getWorldSizeY();

//         int x = (int)indexPosition.x;
//         int y = (int)indexPosition.y;
//         //Comprobamos las celdas que puede observar el dron
//         for(int i=x-2; i<x+2; i++){
//             for(int j=y-2; j<y+2  ; j++){
//                 sensor.AddObservation(trainingTerrain.GetComponent<gridHeat>().isCellOnFire(i, j));
//             }
//         }
//     }

    
//     ///<sumary>
//     /// Funcion que devuelve el angulo entre el vector velocidad y la posicion proyectada 
//     /// en el plano donde se situa el dron.
//     ///</sumary>
//     private float getAngleProyection(Vector3 pos){
//         float angle;

//         Vector3 vectorProyection;
//         if(rigidbody.velocity != Vector3.zero){
//             vectorProyection = new Vector3(pos.x, transform.position.y , pos.z) - transform.position ;
//             //Debug.DrawRay(transform.position, vectorProyection, Color.green);
//             angle = Vector3.SignedAngle(-transform.forward, vectorProyection, Vector3.up);
//             angle = (angle + 180 % 360);
//             //TODO: poner un threshhold para el angulo, no vale que sea 0.001 
   
//             return angle;
//         }

//         return -1;

//     }

//     ///<sumary>
//     /// Comprueba en que celda esta, si no esta en ninguna celda se encuentra fuera de
//     /// terreno. Usamos el velctor -999 como vector null.
//     ///</sumary>
//     private void CheckIfOutoffMap(){
//         Vector2 indexPosition = trainingTerrain.GetComponent<gridHeat>().CellFromWorldPoint(transform.position);
//         if(indexPosition.x <= -999){
//             AddReward(-300f);
//             EndEpisode();
//         }
//         return;
//     }


//     //comprueba si se esta moviendo entre celdas 
//     void ComprobarCambioCelda(){
//         Vector2 pos = trainingTerrain.GetComponent<gridHeat>().WorldPointFromCell(transform.position);
//         if(ultimaCelda != pos){
//             ultimaCelda = pos;
//             AddReward(0.05f);
//         }
//     }  

//     // Fucion qeu comprueba la distacia con el ultimo fuego detectado
//     // y otorga un reward acorde
//     void toFarFromFire(){
//         Vector3 proyectedPos = new Vector3(nearesFirePosition.x, transform.position.y, nearesFirePosition.z);
//         if(Vector3.Distance(transform.position, proyectedPos) < 12){
//             AddReward(+0.02f);
//         }
//     }

//     // Utilizamos el vector direccion y velocidad para asignar un gasto de bateria
//     // Acorde
//     void CheckBatryDrain(){

//     }
    
// }
