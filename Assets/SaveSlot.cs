using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlot : MonoBehaviour {

	public Text description;
	public Text saveDate;

	public void FillData(SaveFile file) {
		if (file == null) {
			description.text = "Empty";
		}
		else {
			description.text = "Save";
			saveDate.text = file.lastSaved;
		}
	}
}
