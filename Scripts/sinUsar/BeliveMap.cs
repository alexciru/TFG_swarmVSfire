using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

//
// Codigo utilizao para construir el Mesh.
// informacion obtenida de: https://catlikecoding.com/unity/tutorials/procedural-grid/
//  con ayuda de : https://www.youtube.com/watch?v=11c9rWRotJ8

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))] //comprueba que tine el componente
[RequireComponent(typeof(MeshRenderer))]
public class BeliveMap : MonoBehaviour{
    Mesh mesh;           // Donde vamos a almacenar el mesh

    public Status[,] values;        // values where we are going to store the values of the believemap

    private Vector3[] vertices;  // lista de vertices del mesh , cada vector3 representa un punto en el espacio
    private int[] triangles;     //lista de los diferentes triangulos, estos estaran formados por 3 vertices
    private Vector2[] uv;
    public int xSizeMap = 20; // Tamaño fijo del mapa
    public int zSizeMap = 20;
    public float cellsize;
    public int xSize;
    public int zSize;
    public float TILESIZE = 0.4f;

    public bool showDroneOnMap = false;
   
    public GameObject DroneIcon;

    ///<sumary>
    /// Funciuon que inicializa el mapa para poder ser utilizado por los drones
    /// o por el scrip del terreno
    ///</sumary>
    public void InizializeMap(){

        int xWorldSize = transform.parent.GetComponent<gridHeat>().getWorldSizeX();
        
        xSize = transform.parent.GetComponent<gridHeat>().getGridSizeX();
        zSize = transform.parent.GetComponent<gridHeat>().getGridSizeY();
        TILESIZE = (float)xSizeMap / (float)xSize;
      
        mesh = new Mesh();


        CreateShape();
        UpdateMesh(); 
        CreateValues(); 
         //TODO: se podria unificar para que solo recorriese el bucle una vez
        //DroneIcon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //GameObject.Destroy(DroneIcon);
        //DroneIcon.transform.localPosition = this.transform.position;

        //DroneIcon.transform.localScale =  new Vector3(1f ,1f, 1f);
        //Renderer r = DroneIcon.GetComponent<Renderer>();
        //r.material.color = Color.red;
    }


    ///<sumary>
    /// Funcion que genera los campos necesarios para el mesh
    ///</sumary>
    public void CreateShape(){
      
        vertices = new Vector3[4*xSize*zSize];
        uv = new Vector2[4*xSize*zSize];
        triangles = new int[6*xSize*zSize];

        int index;
        for(int z = 0; z<zSize; z++){
            for(int x = 0; x<xSize; x++){
                index = z*xSize + x;

                vertices[index * 4 + 0] = new Vector3( z    * TILESIZE , 0, x    * TILESIZE);
                vertices[index * 4 + 1] = new Vector3( z    * TILESIZE , 0,(x+1) * TILESIZE);
                vertices[index * 4 + 2] = new Vector3((z+1) * TILESIZE , 0,(x+1) * TILESIZE);
                vertices[index * 4 + 3] = new Vector3((z+1) * TILESIZE , 0, x    * TILESIZE);

                uv[index * 4 + 0] = new Vector2(0.8f, 0.8f);
                uv[index * 4 + 1] = new Vector2(0.8f, 0.9f);
                uv[index * 4 + 2] = new Vector2(0.9f, 0.9f);
                uv[index * 4 + 3] = new Vector2(0.9f, 0.8f);

                triangles[index * 6 + 0] = index * 4 + 0;
                triangles[index * 6 + 1] = index * 4 + 1;
                triangles[index * 6 + 2] = index * 4 + 2;

                triangles[index * 6 + 3] = index * 4 + 0;
                triangles[index * 6 + 4] = index * 4 + 2;
                triangles[index * 6 + 5] = index * 4 + 3;
            }
        }
  
    }

    ///<sumary>
    /// Funcion que carga los datos creados al objeto Mesh para que pueda ser visualizado
    ///<sumary>
    public void UpdateMesh(){
        //mesh = new Mesh();
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals(); // ajusta iluminacion
        GetComponent<MeshFilter>().mesh = mesh;
    }


    ///<sumary>
    /// Funcion que pasada una celda y un estado se encarga de actualizar el UV , la 
    /// textura del mapa.
    ///<sumary>
    public void UpdateRenderCell(int x, int z, Status estado){
        int index = x*xSize + z;

        switch(estado){
            case Status.OnFire:
                uv[index * 4 + 0] = new Vector2(0.1f  , 0.1f);
                uv[index * 4 + 1] = new Vector2(0.1f  , 0.2f);
                uv[index * 4 + 2] = new Vector2(0.2f, 0.2f);
                uv[index * 4 + 3] = new Vector2(0.2f  , 0.1f);
                break;
            case Status.Suffocated:
                uv[index * 4 + 0] = new Vector2(0.4f, 0.4f);
                uv[index * 4 + 1] = new Vector2(0.4f, 0.5f);
                uv[index * 4 + 2] = new Vector2(0.5f, 0.5f);
                uv[index * 4 + 3] = new Vector2(0.5f, 0.4f);
                break;
            case Status.Normal:
                uv[index * 4 + 0] = new Vector2(0.7f, 0.7f);
                uv[index * 4 + 1] = new Vector2(0.8f, 0.9f);
                uv[index * 4 + 2] = new Vector2(0.9f  , 0.9f);
                uv[index * 4 + 3] = new Vector2(0.9f  , 0.7f);
                break;
        }
    }

    ///<sumary>
    /// Creamos el array de los elememtos para compararlo
    ///<sumary>
    public void CreateValues(){
        values = new Status[xSize, zSize];

        for(int z = 0; z<zSize; z++){
            for(int x = 0; x<xSize; x++){
                values[x, z] = Status.Normal;
            }
        }
    }

    ///<sumary>
    /// Actualizamos el valor de la celda para calculos
    ///<sumary>
    public void UpdateValue(int x, int z, Status estado){
        values[x, z] = estado;
    }

    ///<sumary>
    /// Actualizamos tanto el valor como el render
    ///<sumary>
    public void UpdateCellMap(int x, int z, Status estado){
        UpdateValue(x, z, estado);
        UpdateRenderCell(x, z, estado);
    }


    ///<sumary>
    /// Funcion que se usa para asignar los nuevos valores del UV al
    /// mesh para que pueda ser visualizado los cambios realizados
    ///<sumary>
    public void updateUV(){
        mesh.uv = uv;
    }



    ///<sumary>
    /// Funcion que se utiliza para calclular el datos de similitud entre mapas
    ///<sumary>
    public float CalcularPorcentajeSimilitud(BeliveMap believe){
        Status[,] map = believe.values;
        if(map.Length != xSize*zSize){
            Debug.Log("Error: no coincide el tamaño de los mapas");
            return -1f;
        }
        
        int celdasIguales = 0;
        int celdas = 0;
        for(int z = 0; z<zSize; z++){
            for(int x = 0; x<xSize; x++){
                if(map[x, z] == Status.OnFire){
                    celdas++;
                    if(values[x, z] == map[x, z]) celdasIguales++ ;
                }
            }
        }
        float similitud = ((float)celdasIguales / (float)celdas);
        return similitud;
    }


    public void reset(){
        CreateValues();
    }

}


