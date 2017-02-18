using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PulseImage : MonoBehaviour {

	private Image image;

	// Use this for initialization
	void Start () {
		image = GetComponent<Image>();
	}
	
	// Update is called once per frame
	void Update () {
		image.color = new Color(255,255,255,0.5f+Mathf.PingPong(Time.realtimeSinceStartup,1f)*0.5f);
	}
}
