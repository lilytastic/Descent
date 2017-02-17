using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceObject : MonoBehaviour {

	public int choiceIndex = -1;
	/*
	public Vector2 desiredPosition = new Vector2();
	public RectTransform rect;

	// Use this for initialization
	void Start () {
		rect = GetComponent<RectTransform>();		
	}
	
	// Update is called once per frame
	void Update () {
		if (desiredPosition != Vector2.zero) {
			rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition,desiredPosition,Time.deltaTime*12f);
		}
	}

	public IEnumerator Activate() {
		NovelManager.instance.lastChoice = this;
		GetComponent<Button>().enabled = false;

		float spacing = NovelManager.instance.scriptView.GetComponent<VerticalLayoutGroup>().spacing;
		desiredPosition = new Vector2(NovelManager.instance.scriptView.sizeDelta.x/2,-NovelManager.instance.scriptView.sizeDelta.y-spacing);

		UIFadeIn fade = GetComponent<UIFadeIn>();
		fade.delay = 0.9f;
		fade.target = 0;

		LayoutElement ele = gameObject.GetComponent<LayoutElement>();
		ele.ignoreLayout = true;
		transform.SetParent(transform.parent.parent);

		//yield return new WaitForSeconds(1f);

		//ele.ignoreLayout = false;

		Destroy(gameObject, 5f);

		yield return null;
	}
	*/
}
