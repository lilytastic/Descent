using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Page : MonoBehaviour {

	public RectTransform rect;
	public Vector2 desiredPosition = new Vector2();

	// Use this for initialization
	void Start () {
		rect = GetComponent<RectTransform>();
	}
	
	// Update is called once per frame
	void Update () {
		if (desiredPosition != Vector2.zero) {
			//rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, desiredPosition, Time.deltaTime);
		}
	}
}
