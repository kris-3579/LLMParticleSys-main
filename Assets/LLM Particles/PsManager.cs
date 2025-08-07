using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PsManager : MonoBehaviour
{
    public GameObject PsPrefab;

    // Start is called before the first frame update
    void Start()
    {
        CreatePs(new Vector3(0, 0, 0), "Assets/LLM Particles/Configs/test_output.json");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    // write additional code to read multiple JSON files to run them all 
    // Each file in the folder will run the JSON file system for about 10 or more secs, destroys object, and then will run the next file
    public void CreatePs(Vector3 position, string JsonFilePath)
    {
        GameObject newPs = Instantiate(PsPrefab, position, Quaternion.identity);
        newPs.GetComponent<JSONParticleSys>().LoadAndRun(JsonFilePath);
    }
}
