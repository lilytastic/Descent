using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextBlock : MonoBehaviour {

	public RectTransform rectTransform;
	public HorizontalLayoutGroup horizontalLayoutGroup;
	public CanvasGroup textGroup;
	public Text text;

	private float changeSizeTime = 0.1f;
	private float changeOpacityTime = 1f;
	private float timeSinceSizeChanged = 0;
	private float timeSinceOpacityChanged = 0;

	private Vector2 lastSize = new Vector2();
	private Vector2 desiredSize = new Vector2();

	private float lastOpacity = 0;
	private float desiredOpacity = 0;

	// Use this for initialization
	void OnEnable () {
		StartCoroutine(ResizeandFade());
	}
	
	// Update is called once per frame
	void Update () {
		rectTransform.localScale = Vector3.one;
		if (rectTransform.sizeDelta != desiredSize) {
			timeSinceSizeChanged += Time.deltaTime;
			rectTransform.sizeDelta = Vector2.Lerp(lastSize, desiredSize, timeSinceSizeChanged/changeSizeTime);
		}
		else if (timeSinceSizeChanged != 0) {
			lastSize = rectTransform.sizeDelta;
			timeSinceSizeChanged = 0;
		}

		if (textGroup.alpha != desiredOpacity) {
			timeSinceOpacityChanged += Time.deltaTime;
			//text.color = Color.Lerp(new Color(text.color.a,text.color.g,text.color.b,lastOpacity), new Color(text.color.a, text.color.g, text.color.b, desiredOpacity), timeSinceOpacityChanged);
			textGroup.alpha = Mathf.Lerp(lastOpacity,desiredOpacity,timeSinceOpacityChanged);
		}
		else if (timeSinceOpacityChanged != 0) {
			lastOpacity = textGroup.alpha;
			timeSinceOpacityChanged = 0;
		}
	}

	public IEnumerator ResizeandFade() {
		rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, 0);
		lastSize = rectTransform.sizeDelta;
		yield return new WaitForSeconds(0.2f);
		desiredSize = new Vector2(rectTransform.sizeDelta.x, text.preferredHeight+horizontalLayoutGroup.padding.top+horizontalLayoutGroup.padding.bottom);
		yield return new WaitForSeconds(changeSizeTime);
		desiredOpacity = 1;
		yield return new WaitForSeconds(changeOpacityTime);
		yield return null;
	}
}
