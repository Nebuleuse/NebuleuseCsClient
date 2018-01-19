using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Neb;
using UnityEngine;

public class NebuMgr : MonoBehaviour
{
    int time = 2;
    Nebuleuse neb;
    // Use this for initialization
    void Start()
    {
        neb = new Nebuleuse("http://192.168.0.79:8080", 20);
        //StartCoroutine(neb.Init());

    }

    // Update is called once per frame
    void Update()
    {
        if (time != 0 && time < Time.time)
        {
            time = 0;
            neb.StateCallback = (NebuleuseState state)=>{
                Debug.Log(state);
                if(state == NebuleuseState.NEBULEUSE_CONNECTED){
                    neb.SubscribeTo("game","players", (sucess, response)=>{
                        Debug.Log(response);
                    });
                }
            };
            neb.ErrorCallback = (NebuleuseError error, string message)=>{
                Debug.Log(message);
                Debug.Log(error);
            };
			neb.Init();
            neb.Connect("test", "test");

        }
    }
}
