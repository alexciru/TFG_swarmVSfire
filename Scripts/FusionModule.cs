using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public enum FusionType { MAXIMUM, WEIGHTED, WEIGHTED_SUM };


public class FusionModule : MonoBehaviour{

    public FusionType fusionType;

    //Behaviours
    private droneAgent fireBehaviour;
    private IrBateria bateryBehaviour;
    //private dodgeBehavior esquivar behaviour
    private DroneMovement droneMovement;
    private planningModule planning;
    [Header("Inputs")]
    public Vector3[] velocities;
    public float[] activities;



    [Header("Output")]
    private Vector3 velOut;


    private void Start(){
        
        velocities = new Vector3[3];
        activities = new float[3];
        velocities[2] = Vector3.zero;
        planning = transform.parent.GetComponent<planningModule>();
        
        velOut = new Vector3(0,0,0);

        fireBehaviour = transform.GetComponent<droneAgent>();
        bateryBehaviour = transform.GetComponent<IrBateria>();
        droneMovement = transform.GetComponent<DroneMovement>();
        //esquivarBehaviour = transform.GetComponent<EsquivarBehaviour>();
    }
  

    private void FixedUpdate(){

        //eleegimos el tipo de salida
        velOut = Vector3.zero;
        activities[0] = planning.activity[0];
        activities[1] = planning.activity[1];
        activities[2] = planning.activity[2];
    
        switch(fusionType){
            case FusionType.MAXIMUM:
                velOut = MaximunCalc();
                break;
            case FusionType.WEIGHTED:
                velOut = WeightedCalc();
                break;
            case FusionType.WEIGHTED_SUM:
                velOut = WeightedSumCalc();
                break;
        }

        
        droneMovement.UpdateMovement(velOut);
    }

    // sacamos el maximo segun la actividad
    private Vector3 MaximunCalc(){
        //Debug.Log("fusion: " + activities[0] +" - "+ activities[1]+"- "+ activities[2]);
        if(activities == null) return Vector3.zero;
        float max = activities.Max();
        
        return velocities[activities.ToList().IndexOf(max)];
    }

    //calculo con los pesos
    private Vector3 WeightedCalc(){
        Vector3 velAux = new Vector3();
        for (int i = 0; i < 3; i++){
            velAux.x += activities[i] * velocities[i].x;
            velAux.y += activities[i] * velocities[i].y;
            velAux.z += activities[i] * velocities[i].z;
        }
        velAux.x = velAux.x / activities.Sum();
        velAux.y = velAux.y / activities.Sum();
        velAux.z = velAux.z / activities.Sum();
        return velAux;
    }

    //suma ponderada
    private Vector3 WeightedSumCalc(){
        Vector3 velAux = new Vector3();

        for (int i = 0; i < 3; i++){
            velAux.x += activities[i] * velocities[i].x;
            velAux.y += activities[i] * velocities[i].y;
            velAux.z += activities[i] * velocities[i].z;
        }

        velAux.x = velAux.x / activities.Max();
        velAux.y = velAux.y / activities.Max();
        velAux.z = velAux.z / activities.Max();
        return velAux;
    }

}