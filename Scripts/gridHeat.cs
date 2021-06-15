using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Random=UnityEngine.Random;

public class gridHeat : MonoBehaviour{
    [Header("Experiments")]
    public bool U_shape;
    public bool T_shape;
    public bool Line_Shape;

    private Vector3 worldBottomLeft;      //posicion de la esquina izq 
    private Vector3 worldCentralPosition; //posicion central del terreno
    private Vector2 initialFirePosition;  // posicion inicial del fuego
    [Header("Particules")]
    public bool fireParticleEnable = false; // boleano para iniciar el fuego
    [Header("Opciones del grid")]
    public float cellDiameter = 2;   // tamaño de cada celda
    public int gridWorldSizeX = 100;  // tamaño total del grid
    public int gridWorldSizeY = 100;
    public bool FireInTheCenter = false;
    [Space(10)]

    [Header("Variables en la difusion de temperatura")]
    public float ambient_temperature = 310.0f;// en Kelvin
    public float flame_temperature = 1400.0f; // en Kelvin
    public float thermal_diffusivity = 10f;    // m2 / s 
    public float dx = 26f;                    //TODO: es igual a cellDiameter
    [Space(10)]

    [Header("Combustion")]
    public float ignition_temperature = 561f; // en Kelvin
    public float burn_time_constant = 20;
    public float heat_loss_constant = 200f;   
    public float heating_value_initial = 0.75f;
    private float heating_value;
    [Space(5)]

    [Header("time steps")]
    public float h_initial = 0.05f;
    private float h; // contante de steps
    public float end_time = 30f * 60f;        // 30 min
    public float time_between_steps = 0.01f;  // tiempo entre steps
    [Space(5)]

    [Header("Wind Constant")]
    //TODO: Cambiarlo para que utilice valores de wind gameobject ¿?¿?¿ y combinarlo
    public bool randomWind = true;
    public float velocity_x = 2.5f; //m/s
    public float velocity_y = 0.5f; //m/s
    //[Space(5)]

    [Header("Wood settings")]
    public float min_wood = 20f;
    public float wood_multi = 40f;
    public Gradient wood_gradient; 
    [Space(5)]

    [Header("Color representation")]
    public bool show_temperature = true;
    public Gradient heat_gradient; 
    public float minTemp = 300f;
    public float maxTemp = 1500f;
    [Space(5)]
    public GameObject FireParticle;

    public CellHeat[,] cells;
    int gridSizeX;
    int gridSizeY;


    public bool recordLog = false;
    public int stepToRecord = 1000;
    [Header("Real Map")]
    public GameObject RealmapGameObject;
    private BeliveMap RealMap;

    public GameObject BelievemapGameObject;
    
    [HideInInspector]
    public BeliveMap BelieveMap;

    Vector2 meanFirePosition;
    int fireCount; 
    public List<Transform> trainingDrones = new List<Transform>();

    public bool updateFireValue = true;
    // funcion principal que se ejecuta una vez se inicia la simulacion
    // generando el grid con los valores y llamando a la coorutina
    public void Start() {
        findDronesForTraining();

        if(recordLog) logRecorder.CreateFile();
        this.gridSizeX = Mathf.RoundToInt(gridWorldSizeX/cellDiameter);
        this.gridSizeY = Mathf.RoundToInt(gridWorldSizeY/cellDiameter);

        RealMap = RealmapGameObject.GetComponent<BeliveMap>(); 
        RealMap.InizializeMap();

        BelieveMap = BelievemapGameObject.GetComponent<BeliveMap>(); 
        BelieveMap.InizializeMap();

        
        CreateGrid();
        PlaceGrid();
        resetGridHeat();
    }

    // Funcion que se ocupa de mostrar en el modo escena los cuadrados
    // para el debuggin. Se puede mostrar tanto la temperaura como la cantidad de
    // combustible
    public void OnDrawGizmos() {
        float auxtemp, auxwood, value;
        CellHeat c;
        Gizmos.DrawWireCube(worldCentralPosition, new Vector3(gridWorldSizeX, 0, gridWorldSizeY));
        worldCentralPosition = transform.position + (Vector3.right * gridWorldSizeX/2) + (Vector3.forward * gridWorldSizeY/2) + (Vector3.up * (10 + GetComponent<Terrain>().terrainData.size.y));

        if (cells != null){
            for(int i=0; i<gridSizeY;i++){
                for(int j=0;j<gridSizeY;j++){
                    c = cells[i,j];          
                    Gizmos.DrawCube(c.worldPosition, Vector3.one * (cellDiameter-.1f));
                    if(show_temperature){ 
                        auxtemp = c.temp;
                        value = Mathf.InverseLerp(minTemp, maxTemp, auxtemp); // lo normaliza entre 0 y 1
                        Gizmos.color = heat_gradient.Evaluate(value);
                        if(c.status == Status.Suffocated){
                            Gizmos.color = Color.grey;
                        }
                    }else{// show combustion matrix
                        auxwood = c.wood;
                        value = Mathf.InverseLerp(0, min_wood + wood_multi, auxwood); // lo normaliza entre 0 y 1
                        Gizmos.color = wood_gradient.Evaluate(value);
                        if(c.status == Status.Suffocated){
                            Gizmos.color = Color.black;
                        }
                    }
                }
            }
        }
                
    }

    // Funcion Handler de la corrutina. se encarga de esperar el tiempo
    // necesario y llamar a la funcion que actualiza las temperturas
    public IEnumerator updateFire(){
        int num_steps = (int)(end_time / h);
        for(int i=0; i<=num_steps; i++){
            yield return new WaitForSeconds(time_between_steps);
            heat_conduction();
            updateCells();
     
            if(recordLog) recordTimeStamp(i);
        }
        yield return null;
    }


    // Para solucionar el problema de las coorutina en ML-Agent aumentamos la velocidad del modelo
    // aumentando el valor h del step.
    void ajustValueToTimeScale(){
        h = h_initial * Time.timeScale;
        heating_value = heating_value_initial * Time.timeScale;
    }


    // Funcion encarga de la creacion del encasillado que separa el terreno
    // Entradas: gridSizeX gridSizeY
    void CreateGrid(){
        cells = new CellHeat[gridSizeX, gridSizeY];
        worldCentralPosition = transform.position + Vector3.right * gridWorldSizeX/2 + Vector3.forward * gridWorldSizeY/2 + (Vector3.up * 500) ;
        worldBottomLeft = transform.position;

        for (int x = 0; x<gridSizeX; x++){
            for(int y = 0; y<gridSizeY; y++){
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * cellDiameter + cellDiameter/2) + Vector3.forward * (y * cellDiameter + cellDiameter/2) + (Vector3.up * 10);
                cells[x,y] = new CellHeat(worldPoint, x, y, FireParticle);
            }
        }

    }

    // funcion encargada colocar cada celda a la altura del terreno.
    // Es utilizado posteriormete esta posicion en Y para las particulas del
    // fuego
    private void PlaceGrid(){
        for (int x = 0; x<gridSizeX; x++){
            for(int y = 0; y<gridSizeY; y++){
                cells[x,y].placeCell();
            }
        }
    }


    // Entrada: Vector2: vector2 con coordenadas de la celda
    // Salida: la posicion en el mundo donde se situa
    // Desc: Calcula la celda donde se encuentra la posicion pasada
    //       como entrada.
    public Vector3 WorldPointFromCell(Vector2 cell){
        Vector3 worldPoint;
        float xPosition, yPosition;
        xPosition = worldBottomLeft.x + (cell.x * cellDiameter + cellDiameter/2);
        yPosition = worldBottomLeft.z + (cell.y * cellDiameter + cellDiameter/2);
        worldPoint = new Vector3(xPosition, 0f, yPosition);
        return worldPoint;
    }



    // Obtiene do sposiciones aleatorias y genera un incendio en la celda
    // actuaizamos estado y le actualizamos la cantidad de combustible
    void StartRandomFire(){
        int randX = Random.Range(15, gridSizeX-15); 
        int randY = Random.Range(15, gridSizeY-15);
        if(FireInTheCenter){
            randX = gridSizeX / 2;
            randY = gridSizeY / 2;
        }
        CellHeat c = cells[randX,randY];
        CellHeat c2 = cells[randX-1,randY];
        CellHeat c3 = cells[randX+1,randY];
        CellHeat c4 = cells[randX,randY-1];
        CellHeat c5 = cells[randX,randY+1];
        initialFirePosition = new Vector2(randX, randY);
        //TODO: ponerlo mas bonito, en funciones de celda
        c.status = Status.OnFire;
        c2.status = Status.OnFire;
        c3.status = Status.OnFire;
        c4.status = Status.OnFire;
        c5.status = Status.OnFire;

        c.temp = flame_temperature;
        c2.temp = flame_temperature;
        c3.temp = flame_temperature;
        c4.temp = flame_temperature; 
        c5.temp = flame_temperature;

        if(fireParticleEnable){
            c.SpawnParticle();
            c2.SpawnParticle();
            c3.SpawnParticle();
            c4.SpawnParticle();
            c5.SpawnParticle();
        }

        // Actualizamos el mapa
        RealMap.UpdateRenderCell(randX, randY, Status.OnFire);
        RealMap.UpdateRenderCell(randX-1, randY, Status.OnFire);
        RealMap.UpdateRenderCell(randX+1, randY, Status.OnFire);
        RealMap.UpdateRenderCell(randX, randY-1, Status.OnFire);
        RealMap.UpdateRenderCell(randX, randY+1, Status.OnFire);

        // actualizamos el believe map
        BelieveMap.UpdateCellMap(randX, randY, Status.OnFire);
        BelieveMap.UpdateCellMap(randX-1, randY, Status.OnFire);
        BelieveMap.UpdateCellMap(randX+1, randY, Status.OnFire);
        BelieveMap.UpdateCellMap(randX, randY-1, Status.OnFire);
        BelieveMap.UpdateCellMap(randX, randY+1, Status.OnFire);
        BelieveMap.updateUV();
    }



    // Funcion que se encarga de leer informacion del mapa de combustible
    // y añadir la informacion a la matriz de combustibles 
    // TODO: en vez de perlin noise calcularlo de imagen con UV
    void GenerateCombustionMatrix(){
        float scale = 20;
        float xcoord, ycoord, rand;
        CellHeat c;

        for(int i=0; i < this.gridSizeX; i++){
            for(int j=0; j < this.gridSizeY; j++){
                xcoord = (float) i / gridSizeX * scale;
                ycoord = (float) j / gridSizeY * scale;
                rand =  Mathf.PerlinNoise(xcoord, ycoord);

                c = cells[i,j];
                c.wood = min_wood + (rand * wood_multi);
                c.new_wood = c.wood;
            }
        }

        if(U_shape){
            for(int i=0; i < this.gridSizeX; i++){
                for(int j=gridSizeY/2; j < this.gridSizeY; j++){
                    xcoord = (float) i / gridSizeX * scale;
                    ycoord = (float) j / gridSizeY * scale;

                    c = cells[i,j];
                    c.wood = 0;
                    c.new_wood = c.wood;
                }
            }
        }


        
    }  
    // Inicializa la matriz de las temperaturas. Este valor sera el de las temperatura
    // ambiente.
    void GenerateTemperatureMatrix(){
        foreach(CellHeat cell in cells){
            cell.temp = ambient_temperature;
            cell.new_temp = ambient_temperature;
        }
    }

    // Funciones para almacenar valores de cada tipo de valor
    // utilizado para debugear. Se podra eliminar en el futuro
    void Generate_debug_matrix(){
        foreach(CellHeat c in cells){
                c.difusion = 0f;
                c.ambient_loss = 0f;
                c.wind_loss = 0f;
                c.combustion = 0f;
        }
    }


// la funcion principal para calcular la transmision del calor
    public void heat_conduction(){
        CellHeat c, c_up, c_down, c_left, c_right;
        float burn_rate, temp;
        for(int i=0; i < this.gridSizeX; i++){
            for(int j=0; j < this.gridSizeY; j++){
                c = cells[i,j];
                // caso normal
                if(i>0 && i<gridSizeX-1 && j>0 && j<gridSizeY-1){
                    c_right = cells[i+1, j];
                    c_left = cells[i-1, j];
                    c_up = cells[i, j+1];
                    c_down = cells[i, j-1];
                }else { //Caso borde terreno
                    if(j == 0){
                        c_down = new CellHeat(Vector3.zero,0,0, null);
                        c_up = cells[i, j+1];
                    }else{
                        c_up = new CellHeat(Vector3.zero,0,0, null);
                        c_down = cells[i, j-1];
                    }

                    if(i == 0){
                        c_right = cells[i+1, j];
                        c_left = new CellHeat(Vector3.zero,0,0,null);
                        
                    }else{
                        c_right = new CellHeat(Vector3.zero,0,0,null);
                        c_left = cells[i-1, j];
                    }
                }

                temp = c.temp;
                burn_rate = 0.0f;
                
                if(c.wood <= 10){
                    c.status = Status.Suffocated;
                    c.DestroyParticle();
                }else{
                    if (temp >= ignition_temperature){ 
                        if(c.status != Status.OnFire){
                            c.status = Status.OnFire;
                            if(fireParticleEnable) c.SpawnParticle();
                        }
                        burn_rate = c.wood / burn_time_constant;

                        meanFirePosition += new Vector2(i, j);
                        fireCount ++;
                    }
                    c.new_wood = c.wood - (h* burn_rate);
                }
                // calor por radiacion de las celdas vecinas
                c.difusion = (float)(h *(
                    (thermal_diffusivity / (Mathf.Pow(dx,2))) * (
                        c_down.temp + c_up.temp
                        + c_left.temp + c_right.temp
                        -( 4.0f * temp))));
                // perdida de ambiente
                c.ambient_loss = (float) ((-h *(temp - ambient_temperature) / heat_loss_constant));
                // El viento
                c.wind_loss = (float) (-h* (- 0.5f / dx * (
                    velocity_x * (c_left.temp - c_right.temp)
                    + velocity_y * (c_down.temp - c_up.temp))));
                // temperatura por combustion*/
                c.combustion = (float)(heating_value * burn_rate);
                c.new_temp = temp + c.difusion + c.ambient_loss + c.wind_loss + c.combustion;

                //update belive map
                RealMap.UpdateCellMap(i, j, c.status);
            }
            
        }

        meanFirePosition =  meanFirePosition / fireCount;
        return;
    }


    // Funcion que se encarga de actualizar los valores calculado en la otra matriz
    void updateCells(){
        foreach(CellHeat c in cells){
            c.update_value();
        }
        RealMap.updateUV();
    }


    // Funcion utilizada por el agente para saber si la celda esta en llamas o no
    // La funcion devuelve un boleano indicando si esta o no en llamas
    public bool isCellOnFire(int x, int y){
        if(x>=gridSizeX || y>=gridSizeY || x<0 || y<0)  return false;
        return (cells[x, y].IsCellOnFire());
    }


/*---------------------------------- RESET DEL TERRENO -----------------------------------------------------*/
    // Funcion  que se encarga de resear el valor de las celdas 
    public void resetGridHeat(){
        StopCoroutine("updateFire"); // paramos la coorutina
        resetParticles();
        GenerateCombustionMatrix();
        GenerateTemperatureMatrix();
        Generate_debug_matrix();
        GetRandomWind();
        ResetCellState();
        RealMap.reset();
        BelieveMap.reset();
        ajustValueToTimeScale();

        if(T_shape){
            T_shape_Experiment();
        }else if(Line_Shape){
            Line_shape_Experiment();
        }else{
            StartRandomFire();
        }

        moveDronesToRandomPosition();
        Resources.UnloadUnusedAssets(); // free unused memeory space
        //timer.reset_timer(time_between_steps);
        if(updateFireValue){
            StartCoroutine("updateFire"); // empezar corutina de fuegos
        }
    }


    // Funcion que se encarga de eliminar las particulas de fuego
    public void resetParticles(){
        foreach(CellHeat cell in cells){
            cell.DestroyParticle();
        }
    }

    // Restablece el estado normal a todas las celdas
    public void ResetCellState(){
        foreach(CellHeat cell in cells){
            cell.status = Status.Normal;
        }
    }

    // Crea valores aleatorios para el viento
    public void GetRandomWind(){
        if(randomWind){
            velocity_x = Random.Range(-1f, 1f);
            velocity_y = Random.Range(-1f, 1f);
        }
    }


    // Obtiene los diferentes drones dentro del campo de entenamiento
    public void findDronesForTraining(){
        trainingDrones = new List<Transform>();
        for(int i=0; i< transform.childCount ;i++){
            Transform child = transform.GetChild(i);
            if(child.CompareTag("drone") && child.gameObject.activeInHierarchy){
                trainingDrones.Add(child);
            }
        }
    }

    public List<Transform> findChargeStation(){
        List<Transform> chargeStationList = new List<Transform>();
        Transform estaciones = transform.Find("Estaciones");
        for(int i=0; i< estaciones.childCount ;i++){
            Transform child = estaciones.GetChild(i);
            if(child.CompareTag("charger")){
                chargeStationList.Add(child);
            }
        }
        return chargeStationList;
    }



    // Usamos la lista de drones para colocarles en la posicion inicial para 
    // empezar el episodio
    public void moveDronesToRandomPosition(){
        if(trainingDrones.Count == 0){
            print("No se han detectado ningun drone asignado a este terreno");
            return;
        }
        foreach(Transform drone in trainingDrones){
            //TODO cambiar esto;
            drone.transform.Find("Behaviours").transform.GetComponent<DroneMovement>().MoveToRandomPosition(initialFirePosition);
            //drone.transform.Find("Behaviours").transform.Find("IrBateria").GetComponent<IrBateria>().MoveToRandomPosition(initialFirePosition);
            //drone.transform.Find("Behaviours").transform.Find("BordearFuego").GetComponent<droneAgent>().MoveToRandomPosition(initialFirePosition);
            
        }
    }

    /*--------------------------- GETERS ---------------------------*/

    // Obtiene la la celda segun la posicion pasada por los argumentos
    public Vector2 CellFromWorldPoint(Vector3 world_position){

        float percentX = ((world_position.x - worldBottomLeft.x)  / gridWorldSizeX);
        float percentY = ((world_position.z - worldBottomLeft.z) / gridWorldSizeY);

        if(percentX > 1f || percentX < 0f || percentY > 1f || percentY < 0f ) return new Vector2(-999,-999);

        int i = Mathf.RoundToInt((gridWorldSizeX/cellDiameter) * percentX);
        int j = Mathf.RoundToInt((gridWorldSizeY/cellDiameter) * percentY);
        
        return new Vector2(i,j);
    }



    // Funcion que devuelve la celda dadas las coordenadas pasadas como argumento
    public CellHeat getCellheat(int x , int y){
        if(x>gridSizeX || y>gridSizeY) return null;
        return cells[x,y];
    }

    // Devuelve la posicion media del fuego
    public Vector2 GetMeanFirePosition(){
        return meanFirePosition;
    }

    //TODO: cambiarlo a un sola funcion
    public int getWorldSizeX(){
        return gridWorldSizeX;
    }
    public int getWorldSizeY(){
        return gridWorldSizeY;
    }


    public Vector3 getWorldBottomLeft(){
        return worldBottomLeft;
    }

    // devuelve el vector del viento
    public Vector2 getWind(){
        return new Vector2(velocity_x, velocity_y);
    }

    public float getCellDiameter(){
        return cellDiameter;
    }

    public Vector2 getInitialFirePosition(){
        return initialFirePosition;
    }

    public Vector3 getInitialFirePosition3D(){
        return WorldPointFromCell(new Vector2(initialFirePosition.x, initialFirePosition.y));
        //return new Vector3(initialFirePosition.x, 20, initialFirePosition.y);
    }

    public int getGridSizeX(){
        return gridSizeX;
    }
    public int getGridSizeY(){
        return gridSizeY;
    }

    public void recordTimeStamp(int time){

        Vector3 pos1 = trainingDrones[0].localPosition / cellDiameter;
        Vector3 pos2 = trainingDrones[1].localPosition / cellDiameter;
        Vector3 pos3 = trainingDrones[2].localPosition / cellDiameter;
        Vector3 pos4 = trainingDrones[3].localPosition / cellDiameter;


        float similValue = BelieveMap.CalcularPorcentajeSimilitud(RealMap);
        bool recordFire = ((time % stepToRecord) == 0);
        if((time % 5) == 0) logRecorder.recordTimeStamp(recordFire, gridSizeX, gridSizeY ,time, pos1, pos2, pos3,pos4,  cells, BelieveMap.values, similValue);
    }


    public void Line_shape_Experiment(){

        int middleY = gridSizeY / 2;
        int middleX = gridSizeX / 2;

        CellHeat c = cells[middleX,middleY];
        CellHeat c2 = cells[middleX-1,middleY];
        CellHeat c3 = cells[middleX+1,middleY];
        CellHeat c4 = cells[middleX-2,middleY];
        CellHeat c5 = cells[middleX+2,middleY];
        CellHeat c6 = cells[middleX-3,middleY];
        CellHeat c7 = cells[middleX+3,middleY];
        CellHeat c8 = cells[middleX-4,middleY];
        CellHeat c9 = cells[middleX+4,middleY];
        CellHeat c10 = cells[middleX-5,middleY];
        CellHeat c11 = cells[middleX+5,middleY];

        initialFirePosition = new Vector2(middleX, middleY);
        //TODO: ponerlo mas bonito, en funciones de celda
        c.status = Status.OnFire;
        c2.status = Status.OnFire;
        c3.status = Status.OnFire;
        c4.status = Status.OnFire;
        c5.status = Status.OnFire;
        c6.status = Status.OnFire;
        c7.status = Status.OnFire;
        c8.status = Status.OnFire;
        c9.status = Status.OnFire;
        c10.status = Status.OnFire;
        c11.status = Status.OnFire;

        c.temp = flame_temperature;
        c2.temp = flame_temperature;
        c3.temp = flame_temperature;
        c4.temp = flame_temperature; 
        c5.temp = flame_temperature;
        c6.temp = flame_temperature;
        c7.temp = flame_temperature;
        c8.temp = flame_temperature;
        c9.temp = flame_temperature;
        c10.temp = flame_temperature;
        c11.temp = flame_temperature;

        if(fireParticleEnable){
            c.SpawnParticle();
            c2.SpawnParticle();
            c3.SpawnParticle();
            c4.SpawnParticle();
            c5.SpawnParticle();
            c6.SpawnParticle();
            c7.SpawnParticle();
            c8.SpawnParticle();
            c9.SpawnParticle();
            c10.SpawnParticle();
            c11.SpawnParticle();
        }

        // Actualizamos el mapa
        RealMap.UpdateRenderCell(middleX, middleY, Status.OnFire);
        RealMap.UpdateRenderCell(middleX-1, middleY, Status.OnFire);
        RealMap.UpdateRenderCell(middleX+1, middleY, Status.OnFire);
        RealMap.UpdateRenderCell(middleX-2, middleY, Status.OnFire);
        RealMap.UpdateRenderCell(middleX+2, middleY, Status.OnFire);
        RealMap.UpdateRenderCell(middleX-3, middleY, Status.OnFire);
        RealMap.UpdateRenderCell(middleX+3, middleY, Status.OnFire);
        RealMap.UpdateRenderCell(middleX-4, middleY, Status.OnFire);
        RealMap.UpdateRenderCell(middleX+4, middleY, Status.OnFire);
        RealMap.UpdateRenderCell(middleX-5, middleY, Status.OnFire);
        RealMap.UpdateRenderCell(middleX+5, middleY, Status.OnFire);

        // actualizamos el believe map
        BelieveMap.UpdateCellMap(middleX, middleY, Status.OnFire);
        BelieveMap.UpdateCellMap(middleX-1, middleY, Status.OnFire);
        BelieveMap.UpdateCellMap(middleX+1, middleY, Status.OnFire);
        BelieveMap.UpdateCellMap(middleX-2, middleY, Status.OnFire);
        BelieveMap.UpdateCellMap(middleX+2 ,middleY, Status.OnFire);
        BelieveMap.UpdateCellMap(middleX-3, middleY, Status.OnFire);
        BelieveMap.UpdateCellMap(middleX+3, middleY, Status.OnFire);
        BelieveMap.UpdateCellMap(middleX-4, middleY, Status.OnFire);
        BelieveMap.UpdateCellMap(middleX+4, middleY, Status.OnFire);
        BelieveMap.UpdateCellMap(middleX-5, middleY, Status.OnFire);
        BelieveMap.UpdateCellMap(middleX+5, middleY, Status.OnFire);

        BelieveMap.updateUV();
    }
    

    public void T_shape_Experiment(){

        int middleY = gridSizeY / 2;
        int middleX = gridSizeX / 2;

        CellHeat c = cells[middleX,middleY];
        CellHeat c2 = cells[middleX,middleY-1];
        CellHeat c3 = cells[middleX,middleY+1];
        CellHeat c4 = cells[middleX,middleY+2];
        CellHeat c5 = cells[middleX-1,middleY+2];
        CellHeat c6 = cells[middleX-2,middleY+2];
        CellHeat c7 = cells[middleX+1,middleY+2];
        CellHeat c8 = cells[middleX+2,middleY+2];
        CellHeat c9 = cells[middleX-3,middleY+2];
        CellHeat c10 = cells[middleX+3,middleY+2];
        CellHeat c11 = cells[middleX-4,middleY+2];
        CellHeat c12 = cells[middleX+4,middleY+2];
        CellHeat c13 = cells[middleX,middleY-2];
        CellHeat c14 = cells[middleX,middleY-3];

        initialFirePosition = new Vector2(middleX, middleY);
        //TODO: ponerlo mas bonito, en funciones de celda
        c.status = Status.OnFire;
        c2.status = Status.OnFire;
        c3.status = Status.OnFire;
        c4.status = Status.OnFire;
        c5.status = Status.OnFire;
        c6.status = Status.OnFire;
        c7.status = Status.OnFire;
        c8.status = Status.OnFire;
        c9.status = Status.OnFire;
        c10.status = Status.OnFire;
        c11.status = Status.OnFire;
        c12.status = Status.OnFire;
        c13.status = Status.OnFire;
        c14.status = Status.OnFire;

        c.temp = flame_temperature;
        c2.temp = flame_temperature;
        c3.temp = flame_temperature;
        c4.temp = flame_temperature; 
        c5.temp = flame_temperature;
        c6.temp = flame_temperature;
        c7.temp = flame_temperature;
        c8.temp = flame_temperature;
        c9.temp = flame_temperature;
        c10.temp = flame_temperature;
        c11.temp = flame_temperature;
        c12.temp = flame_temperature;
        c13.temp = flame_temperature;
        c14.temp = flame_temperature;

        if(fireParticleEnable){
            c.SpawnParticle();
            c2.SpawnParticle();
            c3.SpawnParticle();
            c4.SpawnParticle();
            c5.SpawnParticle();
            c6.SpawnParticle();
            c7.SpawnParticle();
            c8.SpawnParticle();
            c9.SpawnParticle();
            c10.SpawnParticle();
            c11.SpawnParticle();
            c12.SpawnParticle();
            c13.SpawnParticle();
            c14.SpawnParticle();
        }

        // Actualizamos el mapa
        RealMap.UpdateRenderCell(middleX, middleY, Status.OnFire);
        RealMap.UpdateRenderCell(middleX, middleY-1, Status.OnFire);
        RealMap.UpdateRenderCell(middleX, middleY+1, Status.OnFire);
        RealMap.UpdateRenderCell(middleX, middleY+2, Status.OnFire);
        RealMap.UpdateRenderCell(middleX-1, middleY+2, Status.OnFire);
        RealMap.UpdateRenderCell(middleX-2, middleY+2, Status.OnFire);
        RealMap.UpdateRenderCell(middleX+1, middleY+2, Status.OnFire);
        RealMap.UpdateRenderCell(middleX+2, middleY+2, Status.OnFire);
        RealMap.UpdateRenderCell(middleX-3, middleY+2, Status.OnFire);
        RealMap.UpdateRenderCell(middleX+3, middleY+2, Status.OnFire);
        RealMap.UpdateRenderCell(middleX-4, middleY+2, Status.OnFire);
        RealMap.UpdateRenderCell(middleX+4, middleY+2, Status.OnFire);
        RealMap.UpdateRenderCell(middleX, middleY-2, Status.OnFire);
        RealMap.UpdateRenderCell(middleX, middleY-3, Status.OnFire);

        // actualizamos el believe map
        BelieveMap.UpdateRenderCell(middleX, middleY, Status.OnFire);
        BelieveMap.UpdateRenderCell(middleX, middleY-1, Status.OnFire);
        BelieveMap.UpdateRenderCell(middleX, middleY+1, Status.OnFire);
        BelieveMap.UpdateRenderCell(middleX, middleY+2, Status.OnFire);
        BelieveMap.UpdateRenderCell(middleX-1, middleY+2, Status.OnFire);
        BelieveMap.UpdateRenderCell(middleX-2, middleY+2, Status.OnFire);
        BelieveMap.UpdateRenderCell(middleX+1, middleY+2, Status.OnFire);
        BelieveMap.UpdateRenderCell(middleX+2, middleY+2, Status.OnFire);
        BelieveMap.UpdateRenderCell(middleX-3, middleY+2, Status.OnFire);
        BelieveMap.UpdateRenderCell(middleX+3, middleY+2, Status.OnFire);
        BelieveMap.UpdateRenderCell(middleX-4, middleY+2, Status.OnFire);
        BelieveMap.UpdateRenderCell(middleX+4, middleY+2, Status.OnFire);
        BelieveMap.UpdateRenderCell(middleX, middleY-2, Status.OnFire);
        BelieveMap.UpdateRenderCell(middleX, middleY-3, Status.OnFire);

        BelieveMap.updateUV();
    }
    

}
