using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class ResizeInterface : MonoBehaviour {

	public RectTransform rect;
	public float minSize = 320;
	public float maxSize = 590;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (rect) {
			if (rect.sizeDelta.x != Screen.width) {
				rect.sizeDelta = new Vector2(Mathf.Clamp(Screen.width,minSize,maxSize),rect.sizeDelta.y);
			}
			/*
			if (rect.sizeDelta.x < minSize || rect.sizeDelta.x > maxSize) {
				rect.sizeDelta = new Vector2(Screen.width,rect.sizeDelta.y);
			}
			*/
		}
	}
}
