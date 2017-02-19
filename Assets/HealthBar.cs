using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour {

	public float health = 30;
	public float healthMax = 30;
	
	public RectTransform _rect;
	public Image _image;

	float maxWidth = 100;

	private float speed = 40;

	void Start() {
		maxWidth = _rect.parent.GetComponent<RectTransform>().rect.width;
	}

	void Update () {
		float relativeWidth = health/healthMax*maxWidth;

		if (_rect.sizeDelta.x != relativeWidth) {
			_image.color = Color.Lerp(_image.color,Color.white,Time.fixedDeltaTime*3);
			if (_rect.sizeDelta.x > relativeWidth) {
				_rect.sizeDelta = new Vector2(_rect.sizeDelta.x-Time.deltaTime*speed,_rect.sizeDelta.y);
				if (_rect.sizeDelta.x < relativeWidth) {_rect.sizeDelta = new Vector2(relativeWidth,_rect.sizeDelta.y);}
			}
			else {
				_rect.sizeDelta = new Vector2(_rect.sizeDelta.x+Time.deltaTime*speed,_rect.sizeDelta.y);
				if (_rect.sizeDelta.x > relativeWidth) {_rect.sizeDelta = new Vector2(relativeWidth,_rect.sizeDelta.y);}
			}
		}
		else {
			_image.color = Color.Lerp(_image.color,new Color(255,255,255,0.64f),Time.fixedDeltaTime*0.4f);
		}
	}
}
