using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceUI : MonoBehaviour {

	// Use this for initialization
	void Start () {
		Color accentColor = FlatUI.instance.accentColor;
		if (accentColor != null) {
			Button b = GetComponent<Button>();
			ColorBlock colorBlock = b.colors;
			colorBlock.normalColor = accentColor;
			b.colors = colorBlock;
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
