using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class ResizeInterface : MonoBehaviour {

	public RectTransform rect;
	public Vector2 minSize = new Vector2(); // 320,400
	public Vector2 maxSize = new Vector2(); // 590,640

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (rect) {
			if (rect.sizeDelta.x != Screen.width) {
				rect.sizeDelta = new Vector2(Mathf.Clamp(Screen.width,minSize.x,maxSize.x),rect.sizeDelta.y);
			}
			if (rect.sizeDelta.y != Screen.height) {
				rect.sizeDelta = new Vector2(rect.sizeDelta.x,Mathf.Clamp(Screen.height,minSize.y,maxSize.y));
			}
			/*
			if (rect.sizeDelta.x < minSize || rect.sizeDelta.x > maxSize) {
				rect.sizeDelta = new Vector2(Screen.width,rect.sizeDelta.y);
			}
			*/
		}
	}
}
