using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ink.Runtime;

public class SaveSlots : MonoBehaviour {

	MainMenu mainMenu = null;

	public enum MenuType {
		Load,
		CreateNew,
		Save
	}
	public MenuType type = MenuType.Load;

	// Use this for initialization
	void OnEnable () {
		mainMenu = GameObject.FindObjectOfType<MainMenu>();
		AddSlots();
	}

	void ClearSlots() {
		for (int i = 0; i < transform.childCount; i++) {
			Destroy(transform.GetChild(i).gameObject);
		}
	}

	void AddSlots() {
		//Debug.Log("Shiggy diggy");
		ClearSlots();

		GameObject saveSlotProto = (GameObject)Resources.Load("Prefabs/Utility/Save Slot");
		for (int i = 0; i < 3; i++) {
			int ind = i;
			GameObject slot = GameObject.Instantiate(saveSlotProto);
			slot.transform.SetParent(transform);

			// Give each cell its functionality
			Button b = slot.GetComponent<Button>();
			switch (type) {
				case MenuType.CreateNew:
					b.onClick.AddListener(() => mainMenu.NewGame(ind));
					if (StoryManager.saveSlots[i] != null) {
						b.enabled = true;
					}
					else {
						b.enabled = true;
					}
					break;
				case MenuType.Save:
					b.onClick.AddListener(() => mainMenu.Save(ind));
					if (StoryManager.saveSlots[i] != null) {
						b.enabled = true;
					}
					else {
						b.enabled = true;
					}
					break;
				case MenuType.Load:
					b.onClick.AddListener(() => mainMenu.Load(ind));
					if (StoryManager.saveSlots[i] != null) {
						b.enabled = true;
					}
					else {
						b.enabled = false;
					}
					break;
			}

			// Choose what to display in cell
			if (StoryManager.saveSlots[i] != null) {
				slot.transform.FindChild("Empty").gameObject.SetActive(false);

				GameObject info = slot.transform.FindChild("Non-Empty").gameObject;
				info.SetActive(true);

				Story duh = new Story(StoryManager.mainStory.text);

				SaveFile save = JsonUtility.FromJson<SaveFile>(StoryManager.saveSlots[i]);
				try { duh.state.LoadJson(save.storyState); }
				catch { }

				//duh.state.LoadJson();
				//Debug.Log(duh.variablesState["room"]);
				Text location = info.transform.FindChild("Location").GetComponent<Text>();
				if (location) { location.text = save.room; }

				Text date = info.transform.FindChild("Date").GetComponent<Text>();
				if (date) { date.text = save.lastSaved; }
			}
			else {
				slot.transform.FindChild("Empty").gameObject.SetActive(true);
				slot.transform.FindChild("Non-Empty").gameObject.SetActive(false);
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
