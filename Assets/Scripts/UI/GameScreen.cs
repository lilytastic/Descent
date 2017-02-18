using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameScreen : MonoBehaviour {

	public CanvasGroup canvasGroup;
	public bool isActive = false;

	public ScreenManager screenManager;

	public GameManager.ScreenState correspondingState;

	// Use this for initialization
	void Start () {
		if (!canvasGroup) {
			canvasGroup = transform.GetOrAddComponent<CanvasGroup>();
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (screenManager) {
			bool wasActive = isActive;
			isActive = (correspondingState == screenManager.state);
			if (!wasActive && isActive) {
				
			}
		}

		canvasGroup.interactable = isActive;
		canvasGroup.blocksRaycasts = isActive;
		if (isActive) {
			canvasGroup.alpha += Time.fixedDeltaTime;
		}
		else {
			canvasGroup.alpha -= Time.fixedDeltaTime;
		}
	}
}
