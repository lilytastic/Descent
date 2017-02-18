using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIFadeIn : MonoBehaviour {

	public CanvasGroup canvasGroup;
	public float fadeTime = 1;
	public float target = 1;
	public float delay = 0;

	// Use this for initialization
	void Start () {
		canvasGroup = GetComponent<CanvasGroup>();
		canvasGroup.alpha = 0;
	}
	
	// Update is called once per frame
	void Update () {
		if (delay > 0) { delay -= Time.deltaTime; }
		else {
			if (canvasGroup.alpha < target) {
				canvasGroup.alpha += Time.deltaTime / fadeTime;
			}
			else if (canvasGroup.alpha > target) {
				canvasGroup.alpha -= Time.deltaTime / fadeTime;
			}
		}
	}
}
