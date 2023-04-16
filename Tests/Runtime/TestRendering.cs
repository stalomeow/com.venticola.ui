using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestRendering : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        print(Camera.main.worldToCameraMatrix);
        print(Camera.main.transform.forward);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
