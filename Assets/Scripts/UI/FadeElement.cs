using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeElement : MonoBehaviour {

	public float target = 1;
	public float speed = 1;

	private CanvasGroup _group;

	// Use this for initialization
	void Start () {
		_group = GetComponent<CanvasGroup>();
	}
	
	// Update is called once per frame
	void Update () {
		//_group.alpha = Mathf.Lerp(_group.alpha,target,Time.fixedDeltaTime*speed);
		if (_group.alpha < target) {_group.alpha += Time.fixedDeltaTime*speed; if (_group.alpha > target) {_group.alpha = target;}}
		if (_group.alpha > target) {_group.alpha -= Time.fixedDeltaTime*speed; if (_group.alpha < target) {_group.alpha = target;}}
	}
}
