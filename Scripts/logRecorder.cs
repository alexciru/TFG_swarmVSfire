using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public static class logRecorder 
{
    public static  string filename = @"D:\Alejandro\UnityProyects\SwarmVsFire\Assets\log_record.txt";
    private static string path;
    private static StreamWriter sw;
 
    private static string dronePositionList = "";
    private static string dronePositionList2 = "";
    private static string dronePositionList3 = "";
     private static string dronePositionList4 = "";
    private static string similitudString = "";

    ///<sumary>
    /// Funcion que crea o abri un archivo para ir almacenando informacion de la simulacion 
    /// para generar graficas
    ///</sumary>
    static public void CreateFile()
    {
        //path = filename.Replace("log_record", System.DateTime.Now.ToString());
        path = filename;
        //File.Delete(path);    
        sw = new StreamWriter(path);

        string line = "Time: 0 ";
        line += "Drone-n: Drone-X; Drone-Y; ";
        line += "Drone-n: Drone-X; Drone-Y; ";
        line += "Incendio: [] ";
        line += "FuelMap : [] ";
        line += "BelieveMap: [] ";
        line += "Similitud: ";
    }

    ///<sumary>
    /// Funcion que se encarga de guardar en un fichero la informacion relevante.
    /// Estado del incendio - estado del belive map - posicion de los drones
    /// incendio: []
    /// believe_map: []
    /// dron1_pos: x,y (info bateria ?)
    /// dron2_pos: x, y (info bateria ? )
    ///</sumary>
    static public void recordTimeStamp(bool ShowFire , int gridSizeX, int gridSizeY, int time, Vector3 Drone1pos, Vector3 Drone2pos , Vector3 Drone3pos,Vector3 Drone4pos, CellHeat[,] cells,  Status[,] beliveMap, float similitud){
        //path = Application.streamingAssetsPath + "\\" + filename + ".csv";
        string fuelMapString = " ";
        string  incendioString = " ";
        string believeString = "";
        if(ShowFire){
            for(int j = 0; j < gridSizeY ; j++){
                for(int i = 0; i < gridSizeX ; i++){
                    CellHeat cell = cells[i,j];
                    fuelMapString += Mathf.RoundToInt(cell.wood) + ",";
                    incendioString += cell.status + ",";
                    believeString += beliveMap[i,j].ToString() +  ",";
                }
            }
        }
        

        string line = " --- Time: " + time+ "\n";
        string pos1 = "["+ Drone1pos.x.ToString().Replace(",", ".") +","+ Drone1pos.z.ToString().Replace(",", ".") + "]";
        string pos2 = "[" + Drone2pos.x.ToString().Replace(",", ".") +","+ Drone2pos.z.ToString().Replace(",", ".") + "]";
        string pos3 = "[" + Drone3pos.x.ToString().Replace(",", ".") +","+ Drone3pos.z.ToString().Replace(",", ".") + "]";
        string pos4 = "[" + Drone4pos.x.ToString().Replace(",", ".") +","+ Drone4pos.z.ToString().Replace(",", ".") + "]";

        similitudString += ",[" + time + "," + similitud.ToString().Replace(",", ".") + "]";

        line += " Drone1: " + pos1 + "\n" ;
        line += " Drone2: " + pos2 + "\n";
        line += " Drone3: " + pos3 + "\n";
        line += " Drone4: " + pos4 + "\n";

        dronePositionList +=  "," + pos1;
        dronePositionList2 += "," + pos2;
        dronePositionList3 += "," + pos3;
        dronePositionList4 += "," + pos4;

        //line += "Drone-" + Droneindex2 + ": Drone-"+ Drone2pos.x +"; Drone-Y;"+ Drone2pos.z ;
        if(ShowFire){
            line += " Incendio: " + incendioString + "\n";
            line += " Combustible: " + fuelMapString + "\n";
            line += " BelieveMap: " + believeString + "\n";
            line += " Similitud: " + similitud + "\n";

            line += "Drone1: " + dronePositionList + "\n";
            line += "Drone2: " + dronePositionList2 + "\n";
            line += "Drone3: " + dronePositionList3 + "\n";
            line += "Drone4: " + dronePositionList4 + "\n";

            line += "Similitud: " + similitudString + "\n";
        }

        sw.WriteLine(line.ToString());
    }


    ///<sumary>
    /// Funcion que cierra el fichero
    ///</sumary>
    static public void closeFile(){
        sw.Close();
    }
}
