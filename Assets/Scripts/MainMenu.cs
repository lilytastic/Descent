using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ink.Runtime;

public class MainMenu : MonoBehaviour {

	public GameObject continueButton;
	public bool controlCamera = false;

	public enum MenuState {
		Title,
		Main,
		New,
		Load,
		Save,
		Options
	}
	public MenuState currentState = MenuState.Title;
	public MenuState defaultState = MenuState.Title;

	public GameObject menuTitle;
	public GameObject menuMain;
	public GameObject menuNew;
	public GameObject menuSave;
	public GameObject menuLoad;
	public GameObject menuOption;

	public Vector3 desiredCameraRotation = new Vector3(-10f, 0, 0);

	IEnumerator ChangeState(MenuState _state) {
		//if (currentState == _state) { yield break; }
		MenuState previousState = currentState;
		GameObject currentMenu = null;
		float fadeTime = 0.4f;
		float steps = 100;

		if (menuTitle && currentState == MenuState.Title) { currentMenu = menuTitle; }
		if (menuMain && currentState == MenuState.Main) { currentMenu = menuMain; }
		if (menuNew && currentState == MenuState.New) { currentMenu = menuNew; }
		if (menuLoad && currentState == MenuState.Load) { currentMenu = menuLoad; }
		if (menuSave && currentState == MenuState.Save) { currentMenu = menuSave; }

		currentState = _state;
		CanvasGroup group = null;
		if (currentMenu != null) {
			group = currentMenu.GetComponent<CanvasGroup>();

			if (group) {
				for (int i = 0; i < steps; i++) {
					group.alpha = 1 - (i / steps);
					yield return new WaitForSeconds(fadeTime / steps);
				}
			}
		}
		currentMenu = null;
		group = null;

		if (menuTitle) { menuTitle.SetActive(currentState == MenuState.Title); }
		if (menuMain) { menuMain.SetActive(currentState == MenuState.Main); }
		if (menuNew) { menuNew.SetActive(currentState == MenuState.New); }
		if (menuLoad) { menuLoad.SetActive(currentState == MenuState.Load); }
		if (menuSave) { menuSave.SetActive(currentState == MenuState.Save); }

		if (menuTitle && currentState == MenuState.Title) {
			currentMenu = menuTitle;
			desiredCameraRotation = new Vector3(-10f, 0, 0);
		}
		if (menuMain && currentState == MenuState.Main) {
			currentMenu = menuMain;
			desiredCameraRotation = new Vector3(-10f, 0, 0);
		}
		if (menuNew && currentState == MenuState.New) {
			currentMenu = menuNew;
			desiredCameraRotation = new Vector3(-80f, 0, 0);
		}
		if (menuLoad && currentState == MenuState.Load) {
			currentMenu = menuLoad;
			desiredCameraRotation = new Vector3(-80f, 0, 0);
		}
		if (menuLoad && currentState == MenuState.Save) {
			currentMenu = menuSave;
			desiredCameraRotation = new Vector3(-80f, 0, 0);
		}

		Vector3 startingCameraRotation = Camera.main.transform.eulerAngles;
		if (currentMenu != null) {
			group = currentMenu.GetComponent<CanvasGroup>();
			if (group) {
				//Debug.Log(group.name);
				group.alpha = 0;
				for (int i = 0; i < steps; i++) {
					group.alpha = i / steps;
					yield return new WaitForSeconds(fadeTime / steps);
				}
			}
		}

		yield return null;
	}

	public void ChangeMenu(string state) {
		MenuState _state = MenuState.Main;
		if (state == "New") { _state = MenuState.New; }
		if (state == "Load") { _state = MenuState.Load; }
		if (state == "Save") { _state = MenuState.Save; }
		if (state == "Options") { _state = MenuState.Options; }
		StartCoroutine(ChangeState(_state));
	}

	void OnEnable() {
		if (StoryManager.mostRecentSave == -1) {
			if (continueButton) { continueButton.SetActive(false); }
		}
		if (menuTitle) { menuTitle.SetActive(false); }
		if (menuMain) { menuMain.SetActive(false); }
		if (menuNew) { menuNew.SetActive(false); }
		if (menuLoad) { menuLoad.SetActive(false); }
		if (menuSave) { menuSave.SetActive(false); }

		StartCoroutine(ChangeState(defaultState));
	}

	void Update() {
		if (controlCamera) { Camera.main.transform.rotation = Quaternion.Slerp(Camera.main.transform.rotation, Quaternion.Euler(desiredCameraRotation), Time.fixedDeltaTime); }

		switch (currentState) {
			case MenuState.Title:
				if (Input.anyKeyDown || Input.GetMouseButtonDown(0)) {
					StartCoroutine(ChangeState(MenuState.Main));
					return;
				}
				break;
			case MenuState.Main:
				if (Input.GetKeyDown(KeyCode.Escape)) {
					StartCoroutine(ChangeState(MenuState.Title));
					return;
				}
				break;
			case MenuState.Load:
				if (Input.GetKeyDown(KeyCode.Escape)) {
					StartCoroutine(ChangeState(MenuState.Main));
					return;
				}
				break;
		}
	}

	public void NewGame(int ind) {
		StoryManager.story.ResetState();
		StoryManager.currentSaveSlot = ind;
		SceneManager.LoadScene("main");
	}
	public void Continue () {
		if (StoryManager.inProgress) {
			SceneManager.LoadScene("main");
		}
		else if (StoryManager.mostRecentSave != -1) {
			Load(StoryManager.mostRecentSave);
		}
	}

	public void Save(int index) {
		StoryManager.currentSaveSlot = index;
		/*
		string json = StoryManager.story.state.ToJson();
		Debug.Log(json);

		StreamWriter sr = File.CreateText("Saves/save" + (StoryManager.currentSaveSlot != -1 ? (StoryManager.currentSaveSlot + 1).ToString() : "1") + ".txt");
		sr.Write(json);
		sr.Close();
		*/

		string json = StoryManager.CreateSaveFile().ToJson();//StoryManager.story.state.ToJson();
		Debug.Log(json);

		StreamWriter sr = File.CreateText("Saves/save" + (StoryManager.currentSaveSlot != -1 ? (StoryManager.currentSaveSlot + 1).ToString() : "1") + ".txt");
		sr.Write(json);
		sr.Close();

		StoryManager.saveSlots[index] = json;

		StartCoroutine(ChangeState(MenuState.Main));
		//StoryManager.LoadSaves();
	}

	public void Load(int index) {
		StoryManager.LoadSaveFile(index);
	}
}
