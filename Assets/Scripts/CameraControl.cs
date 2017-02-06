using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour {

	private Camera camera = null;

	public Transform relativeTo = null;
	public Vector3 desiredVector = new Vector3(-9,13,-9);
	public float desiredOrthographicSize = 10;

	// Use this for initialization
	void Start () {
		camera = transform.GetComponent<Camera>();
	}
	
	// Update is called once per frame
	void LateUpdate () {
		if (relativeTo) {
			transform.position = relativeTo.position + desiredVector;
			camera.orthographicSize = desiredOrthographicSize;
		}
	}
}
