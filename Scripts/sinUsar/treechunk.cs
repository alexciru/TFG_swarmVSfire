/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class treechunk : MonoBehaviour
{

    public int n_squares = 3;
    float[] y_chunks;
    float dis = 1;

    // Start is called before the first frame update
    void Start()
    {
        print(GetComponent<Renderer>().bounds.size);
        y_chunks = new float[n_squares];
        divideTree();


        Tree tree = gameObject.GetComponent<Tree>();
        print(tree.data);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    

    // Divide into chunks
    public void divideTree(){
        Vector3 size = GetComponent<Renderer>().bounds.size;

        this.dis = size.y/n_squares; //se supone que es un cuadrado
 
        Vector3 position = GetComponent<Transform>().position;
        float offset = this.dis/2;
        for(int i=0; i<n_squares; i++){
            Vector3 aux = new Vector3(position.x, position.y + offset, position.z);
            y_chunks[i] = position.y + offset; // lo guardar en la lista
            //Gizmos.DrawWireCube( aux, Vector3.one*dis);
            offset += this.dis;
        }
    }

    //show the chunks in editor
    public void OnDrawGizmos() {
        if(y_chunks != null){
            Vector3 position = GetComponent<Transform>().position;
            for(int i = 0; i<n_squares; i++){
                Vector3 aux = new Vector3(position.x, y_chunks[i], position.z);
                Gizmos.DrawWireCube( aux, Vector3.one*dis);
            }
        }
    }


}
*/