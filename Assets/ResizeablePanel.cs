using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResizeablePanel : MonoBehaviour {

	public Vector2 desiredSize = new Vector2();
	RectTransform rectTransform;

	// Use this for initialization
	void Start () {
		rectTransform = GetComponent<RectTransform>();
	}

	// Update is called once per frame
	void Update() {
		if (desiredSize != Vector2.zero) {
			rectTransform.sizeDelta = Vector2.Lerp(rectTransform.sizeDelta, desiredSize, Time.deltaTime * 3f);
			if (Mathf.Abs(rectTransform.sizeDelta.y - desiredSize.y) < 1) { rectTransform.sizeDelta = desiredSize; }
		}
	}
}
