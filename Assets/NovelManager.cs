using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Ink.Runtime;

public class NovelManager : MonoBehaviour {

	bool inProgress = false;
	bool pause = false;

	List<string> linesToWrite = new List<string>();

	StoryManager.LineFormat lastLine = StoryManager.LineFormat.Action;
	string lastSpeaker = "";
	Entity currentSpeaker = null;

	public ChoiceObject lastChoice = null;
	public List<Transform> choices = new List<Transform>();

	public ScrollRect scroll;
	public RectTransform scrollTransform;
	public RectTransform scriptView;

	public RectTransform choiceAnchor;
	public GameObject textBlockAnchor;

	public GameObject textPrefab;
	public GameObject choicePrefab;
	public GameObject textBlockPrefab;

	public List<GameObject> pages = new List<GameObject>();

	Rect lastChoicePosition;

	static NovelManager _instance;
	private static object _lock = new object();
	static public bool isActive { get { return _instance != null; } }
	static public NovelManager instance {
		get {
			lock (_lock) {
				if (_instance == null) {
					Debug.Log("[Singleton] Creating Singleton: " + FindObjectsOfType(typeof(NovelManager)).Length.ToString());

					_instance = UnityEngine.Object.FindObjectOfType(typeof(NovelManager)) as NovelManager;

					if (FindObjectsOfType(typeof(NovelManager)).Length > 1) {
						Debug.LogError("[Singleton] Something went really wrong " +
							" - there should never be more than 1 singleton!" +
							" Reopening the scene might fix it.");
						//DontDestroyOnLoad(_instance.gameObject);
						return _instance;
					}

					if (_instance == null) {
						/*
						GameObject go = new GameObject("StoryManager");
						//DontDestroyOnLoad(go);
						_instance = go.AddComponent<NovelManager>();
						Debug.Log("[Singleton] An instance of " + typeof(NovelManager) +
							" is needed in the scene, so '" + go.name +
							"' was created with DontDestroyOnLoad.");
						*/
					}
					else {
						Debug.Log("[Singleton] Using instance already created: " +
							_instance.gameObject.name);
					}
				}
				return _instance;
			}
		}
	}
	
	// Use this for initialization
	void Start () {
		//StartCoroutine(WritePage());
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void NewGame() {
		StartCoroutine(WritePage());
	}

	string HandleDialogue(string text) {
		string[] tokens = text.Split(':');
		if (tokens.Length > 1) {
			string dialogue = tokens[1];
			string speaker = tokens[0];
			speaker = speaker[0].ToString().ToUpper() + speaker.Substring(1).ToLower();

			if (dialogue.EndsWith(".")) { dialogue = dialogue.Remove(dialogue.Length-1).Trim()+","; }
			text = "“" + dialogue.Trim() + "”";
			if (Random.Range(0, 100) < 20) { text += " says " + speaker + "."; }
			else { text += " " + speaker + " says."; }
		}
		return text;
	}

	IEnumerator WritePage(string prepend = "") {
		Debug.Log("Pls");
		GameObject go = GameObject.Instantiate(textBlockPrefab);
		pages.Add(go);
		go.transform.SetParent(textBlockAnchor.transform);
		Text t = go.GetComponentInChildren<Text>();

		string page = "";
		if (prepend != "") { page = prepend + "\n\n"; }
		while (StoryManager.story.canContinue) {
			string line = StoryManager.story.Continue().Trim();
			StoryManager.LineFormat format = StoryManager.GetFormat(line);
			if (format == StoryManager.LineFormat.Dialogue) { line = HandleDialogue(line); }
			page += line + "\n\n";
		}
		t.text = page.Trim()+"\n";

		choiceAnchor.SetAsLastSibling();

		StartCoroutine(LoadChoices());

		
		/*
		GameObject go = GameObject.Instantiate((GameObject)Resources.Load("Prefabs/Page"));
		go.transform.SetParent(scriptView.transform);
		Text t = go.GetComponentInChildren<Text>();

		choiceAnchor.SetAsLastSibling();

		string page = "";
		if (prepend != "") { page = prepend + "\n"; }
		while (StoryManager.story.canContinue) {
			string line = StoryManager.story.Continue().Trim();
			StoryManager.LineFormat format = StoryManager.GetFormat(line);
			if (format == StoryManager.LineFormat.Dialogue) { line = HandleDialogue(line); }
			page += line + "\n";
		}

		t.text = page.Trim();
		ResizeablePanel lastChoicePanel = null;
		if (lastChoice) {
			lastChoicePanel = lastChoice.GetComponent<ResizeablePanel>();
			if (!lastChoicePanel) { lastChoicePanel = lastChoice.gameObject.AddComponent<ResizeablePanel>(); }
		}
		UIFadeIn fade = go.GetComponent<UIFadeIn>();
		fade.delay = 0.3f;

		yield return new WaitForSeconds(0.2f);

		HorizontalLayoutGroup hlg = go.GetComponent<HorizontalLayoutGroup>();
		float padding = 0; if (hlg) { padding += hlg.padding.top + hlg.padding.bottom; }
		if (lastChoice) {
			go.GetComponent<ResizeablePanel>().desiredSize = new Vector2(800, t.preferredHeight + 5 + padding);
			if (lastChoice) { lastChoice.rect.sizeDelta = new Vector2(lastChoicePanel.GetComponent<RectTransform>().sizeDelta.x, t.preferredHeight + 5 + padding); }
		}
		else {
			go.GetComponent<RectTransform>().sizeDelta = new Vector2(800, t.preferredHeight + 5 + padding);
		}
		choiceAnchor.GetComponent<ResizeablePanel>().desiredSize = new Vector2(800, StoryManager.story.currentChoices.Count * 51f);

		yield return new WaitForSeconds(0.2f);

		RectTransform rect = go.GetComponent<RectTransform>();
		//rect.anchoredPosition = new Vector2(scriptView.sizeDelta.x / 2, rect.sizeDelta.y);		
		choiceAnchor.SetAsLastSibling(); ;
		
		StartCoroutine(LoadChoices());

		yield return new WaitForSeconds(0.2f);

		Page p = go.GetComponent<Page>();
		//p.desiredPosition = new Vector2(p.rect.anchoredPosition.x, (-scriptView.sizeDelta.y)+p.rect.sizeDelta.y);
		scroll.verticalNormalizedPosition = 0;
		yield return new WaitForSeconds(6f);

		int maxPages = 5;
		if (scriptView.transform.childCount > maxPages) {
			for (int i = 0; i < scriptView.transform.childCount - maxPages; i++) {
				Destroy(scriptView.transform.GetChild(i).gameObject);
			}
		}
		*/
		yield return null;
	}

	IEnumerator ChangeScene(string _scene) {
		yield return null;
	}

	IEnumerator LoadChoices() {
		Debug.Log("Loading Choices");
		if (StoryManager.story.currentChoices.Count > 0) {
			for (int i = 0; i < StoryManager.story.currentChoices.Count; ++i) {
				Choice choice = StoryManager.story.currentChoices[i];
				//Debug.Log("Choice " + (i + 1) + ". " + choice.text);

				int ind = i;
				GameObject choiceObj = GameObject.Instantiate(choicePrefab);
				choices.Add(choiceObj.transform);
				choiceObj.transform.SetParent(choiceAnchor);

				RectTransform rect = choiceObj.GetComponent<RectTransform>();
				rect.anchoredPosition = new Vector2(10*i,rect.anchoredPosition.y);

				UIFadeIn fade = choiceObj.GetComponent<UIFadeIn>();
				if (fade) { fade.delay = 0.7f + 0.1f * i; }

				Text _t = choiceObj.GetComponentInChildren<Text>();
				_t.text = choice.text;
				if (_t.text.StartsWith("\"") || _t.text.StartsWith("“")) {
					_t.text = "“" + _t.text.Remove(0,1) + "”";
				}
				else {
					//_t.text = "[" + _t.text + "]";
				}

				//rect.anchoredPosition = new Vector2(scriptView.sizeDelta.x / 2, -55*i);
				//_t.text = "> " + _t.text;
				//_t.rectTransform.sizeDelta = new Vector2(viewWidth, 15);
				Button b = choiceObj.GetComponent<Button>();
				b.onClick.AddListener(() => StartCoroutine(SelectChoice(ind)));
			}
		}
		yield return null;
	}

	IEnumerator SelectChoice(int opt) {
		string fullText = StoryManager.story.currentChoices[opt].text.Trim();
		StoryManager.story.ChooseChoiceIndex(opt);
		for (int i = 0; i < choices.Count; i++) {
			Destroy(choices[i].gameObject);
		}
		choices.Clear();
		
		string prepend = "";
		if (fullText.StartsWith("\"") || fullText.StartsWith("“")) {
			//linesToWrite.Add("ANANTH: " + fullText.Remove(0, 1));
			//prepend = HandleDialogue("ANANTH: " + fullText.Remove(0, 1));
		}

		//choiceAnchor.GetComponent<ResizeablePanel>().desiredSize = new Vector2(800, 0);
		
		yield return StartCoroutine(WritePage(prepend));
		//yield return StartCoroutine(ScrollToBottomGradual());
		yield break;
	}

	public static Rect RectTransformToScreenSpace(RectTransform transform) {
		Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
		float x = transform.position.x + transform.anchoredPosition.x;
		float y = Screen.height - transform.position.y - transform.anchoredPosition.y;

		return new Rect(x, y, size.x, size.y);
	}
}
