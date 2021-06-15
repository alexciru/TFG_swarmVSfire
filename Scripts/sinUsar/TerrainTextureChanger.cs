using UnityEngine;
using System.Collections;

public class TerrainTextureChanger : MonoBehaviour{
    public Terrain terrain;

    // si se puslsa espacio se cambia la textura entera
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)){   
            print("pulsado space");
            UpdateTerrainTexture(terrain.terrainData, 0, 1);
        }
        if (Input.GetKeyUp(KeyCode.Space)){
            //switch all painted in texture 2 to texture 1
            UpdateTerrainTexture(terrain.terrainData, 1, 0);
        }
    }

    // Funcion que se encarga de cambiar la textura entera 
    // se puede obtener informacion sobre el punto que nos interesa modificar
    static void UpdateTerrainTexture(TerrainData terrainData, int textureNumberFrom, int textureNumberTo){
        //obtenemos la mascara actual
        float[, ,] alphas = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);

        // make sure every grid on the terrain is modified
        //print(terrainData.alphamapWidth); 512
        //print(terrainData.alphamapHeight); 512
        for (int i = 0; i < terrainData.alphamapWidth; i++){
            for (int j = 0; j < terrainData.alphamapHeight; j++){
                //for each point of mask do:
                //paint all from old texture to new texture (saving already painted in new texture)
                alphas[i, j, textureNumberTo] = Mathf.Max(alphas[i, j, textureNumberFrom], alphas[i, j, textureNumberTo]);
                //set old texture mask to zero
                alphas[i, j, textureNumberFrom] = 0f;
            }
        }
        // apply the new alpha
        terrainData.SetAlphamaps(0, 0, alphas);
    }
}