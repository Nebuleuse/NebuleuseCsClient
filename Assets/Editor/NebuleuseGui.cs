using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Neb;
using System.Threading;

public class NebuleuseGui : EditorWindow {

	[MenuItem ("Window/Nebuleuse")]

    public static void  ShowWindow () {
        EditorWindow.GetWindow(typeof(NebuleuseGui));
        
    }
    
    void OnGUI () {
        // The actual window code goes here
        if(GUILayout.Button("Build Object"))
        {

            Debug.Log(Thread.CurrentThread.ManagedThreadId);
            Debug.Log(Time.time);
            Nebuleuse neb = new Nebuleuse("http://127.0.0.1:8080", 20);
            neb.Init();
        }
    }
}
