using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class automove : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		var pos = transform.position;
		pos.x += Mathf.Sin(Time.time) * Time.deltaTime;
		pos.y += Mathf.Cos(Time.time) * Time.deltaTime;
		transform.position = pos;
	}
}
