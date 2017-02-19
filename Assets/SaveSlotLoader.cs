using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotLoader : MonoBehaviour {

	public GameObject saveSlotPrefab;
	public enum SaveSlotType {
		Save,
		Load,
		Delete
	}
	public SaveSlotType slotType = SaveSlotType.Save;

	public void LoadSlots () {
		transform.DestroyChildren();
		ScreenManager _manager = null;
		GameScreen parentScreen = transform.parent.GetComponent<GameScreen>();
		if (parentScreen) {_manager = parentScreen.screenManager;}
		for (int i = 0; i < StoryManager.saveSlots.Length; i++) {
			GameObject slot = GameObject.Instantiate(saveSlotPrefab);
			slot.transform.SetParent(transform);
			Button b = slot.transform.GetOrAddComponent<Button>();
			SaveFile _file = StoryManager.saveSlots[i];
			
			SaveSlot _slot = slot.transform.GetOrAddComponent<SaveSlot>();
			_slot.FillData(_file);

			int ind = i;
			if (slotType == SaveSlotType.Save) {
				b.onClick.AddListener(() => StoryManager.instance.SaveTo(ind));
			}
			else {
				b.onClick.AddListener(() => StoryManager.instance.LoadSaveFile(_file));
			}
			if (_manager) {
				b.onClick.AddListener(() => _manager.ChangeState("Main"));
			}
		}
	}
}
