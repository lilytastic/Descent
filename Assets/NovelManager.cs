using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Ink.Runtime;

public class NovelManager : MonoBehaviour {

	bool inProgress = false;
	bool pause = false;

	List<string> linesToWrite = new List<string>();

	private string[] silentOptions = new string[] {"...","say nothing","remain silent"};

	StoryManager.LineFormat lastLine = StoryManager.LineFormat.Action;
	string lastSpeaker = "";
	Entity currentSpeaker = null;

	public ChoiceObject lastChoice = null;
	public GameObject lastTextBlock = null;
	public List<Transform> choices = new List<Transform>();
	public List<Choice> pendingChoices = new List<Choice>();

	public bool multiblock = true;
	public bool indent = false;
	public bool continueMaximally = false;
	public float scrollSpeed = 3; // -1 for insta-scroll

	public ScrollRect scroll;
	public RectTransform scrollTransform;
	public RectTransform scriptView;

	public RectTransform choiceAnchor;
	public GameObject textBlockAnchor;

	public GameObject textPrefab;
	public GameObject choicePrefab;
	public GameObject textBlockPrefab;

	public List<GameObject> pages = new List<GameObject>();

	List<StoryLine> lines = new List<StoryLine>();
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
		NewGame();
		//StartCoroutine(WritePage());
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetMouseButtonDown(0)) {
			StartCoroutine(Next());
		}
	}

	public void NewGame() {
		ClearText();
		ClearChoices();
		
		StartCoroutine(Next());
		//StartCoroutine(WritePage());
	}

	public void ClearText() {
		for (int i = 0; i < textBlockAnchor.transform.childCount; i++) {
			Transform t = textBlockAnchor.transform.GetChild(i);
			if (t != choiceAnchor) {Destroy(t.gameObject);}
		}
		for (int i = 0; i < pages.Count; i++) {
			if (pages[i].gameObject) {Destroy(pages[i].gameObject);}
		}
		pages.Clear();
	}
	public void ClearChoices() {
		for (int i = 0; i < choiceAnchor.transform.childCount; i++) {
			Transform t = choiceAnchor.transform.GetChild(i);
			Destroy(t.gameObject);
		}
		for (int i = 0; i < choices.Count; i++) {
			if (choices[i]!=null && choices[i].gameObject) {Destroy(choices[i].gameObject);}
		}
		choices.Clear();
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

	List<StoryLine> GetLines() {
		List<StoryLine> lines = new List<StoryLine>();
		//int _lines = 0;
		while (StoryManager.story.canContinue) {
			string line = StoryManager.story.Continue().Trim();
			StoryLine _line = new StoryLine();
			_line.text = HandleLine(line);
			_line.tags = StoryManager.story.currentTags.ToArray();
			lines.Add(_line);
		}	
		return lines;	
	}

	List<Choice> GetChoices(int choiceIndex) {
		List<Choice> _choices = new List<Choice>();
		_choices.Add(StoryManager.story.currentChoices[choiceIndex]);
		return _choices;
	}

	string CleanText(string s) {
		return s.Trim().ToLower().Replace("!","");
	}

	IEnumerator Next() {
		GameObject duh = EventSystem.current.currentSelectedGameObject;
		//Debug.Log(duh ? duh.name : "No object");
		if (duh && duh.GetComponent<Button>()) {yield return null;}
		else {
			if (lines.Count == 0 && StoryManager.story.canContinue) {
				lines = GetLines();
				pendingChoices = StoryManager.story.currentChoices;
			}
			else if (lines.Count == 0 && !StoryManager.story.canContinue) {
				//Debug.Log("DOOP");
				foreach (Choice c in StoryManager.story.currentChoices) {
					string cleanText = CleanText(c.text);
					//Debug.Log(cleanText);
					if (silentOptions.Contains(cleanText)) {
						Debug.Log(":)");
						StartCoroutine(SelectChoice(c.index));
					}
				}
			}
			//Debug.Log(lines.Count);
			if (lines.Count > 0) {
				StoryLine line = lines[0];
				lines.RemoveAt(0);
				//Debug.Log(line.text);
				StartCoroutine(WriteLine(line));
				if (lines.Count == 0) {
					StartCoroutine(LoadChoices(pendingChoices));
					pendingChoices.Clear();
					//LoadChoices();
				}
			}
		}

		yield return null;
	}

	IEnumerator ManageTextBlock() {		
		GameObject go = null;
		float height = 0;

		if (multiblock && lastTextBlock != null) {
			FadeElement _fade = lastTextBlock.GetComponent<FadeElement>();
			if (_fade && _fade.target > 0.65f) {_fade.target = 0.65f;}
		}
	
		foreach (GameObject page in pages) {
			if (page != null) {
				RectTransform _r = page.GetComponent<RectTransform>();
				height += _r.sizeDelta.y;
			}
		}
		if (multiblock || lastTextBlock==null) {
			if (!multiblock) {
				foreach (GameObject page in pages) {FadeElement _fade = page.GetComponent<FadeElement>(); _fade.target = 0;}
				yield return new WaitForSeconds(1f);				
				ClearText(); 
				scroll.verticalNormalizedPosition = 1;
				yield return new WaitForSeconds(0.5f);
			}
			go = GameObject.Instantiate(textBlockPrefab);
			go.GetComponentInChildren<Text>().text = "";
			pages.Add(go);
			go.transform.SetParent(textBlockAnchor.transform);
			lastTextBlock = go;			
			lastTextBlock.GetComponent<CanvasGroup>().alpha = 0;
			lastTextBlock.GetComponent<FadeElement>().target = 1;
		}
		else {
			go = lastTextBlock;
		}
		choiceAnchor.SetAsLastSibling();

		Canvas.ForceUpdateCanvases();
		RectTransform r = lastTextBlock.GetComponent<RectTransform>();
		float pos = r.anchoredPosition.y+r.sizeDelta.y;
		float yOffset = 0;
		float totalHeight = scriptView.sizeDelta.y;
		float offset = (pos+yOffset)/totalHeight;
		//Debug.Log(offset);
		float scrollPoint = offset;
		scroll.verticalNormalizedPosition = offset;
		//StartCoroutine(ScrollTo(pos));
	}

	IEnumerator ScrollTo(float pos) {		
		for (float i = 0; i < 1; i+=Time.fixedDeltaTime) {
			float offset = pos/scriptView.sizeDelta.y;
			if (scroll.verticalNormalizedPosition == offset) {break;}
			if (scroll.verticalNormalizedPosition > offset) {
				scroll.verticalNormalizedPosition += Time.fixedDeltaTime;
				if (scroll.verticalNormalizedPosition < offset) {scroll.verticalNormalizedPosition = offset;}
			}
			else if (scroll.verticalNormalizedPosition < offset) {
				scroll.verticalNormalizedPosition -= Time.fixedDeltaTime;
				if (scroll.verticalNormalizedPosition > offset) {scroll.verticalNormalizedPosition = offset;}
			}
			scroll.verticalNormalizedPosition = offset;
			yield return new WaitForSeconds(Time.fixedDeltaTime);
		}
	}

	IEnumerator WriteLine(StoryLine line) {
		List<Choice> _choices = StoryManager.story.currentChoices;
		yield return ManageTextBlock();
		
		GameObject go = lastTextBlock;
		Text t = go.GetComponentInChildren<Text>();
		RectTransform r = lastTextBlock.GetComponent<RectTransform>();
		CanvasGroup g = lastTextBlock.GetComponent<CanvasGroup>();
		lastTextBlock.GetComponent<FadeElement>().target = 1;

		t.text = line.text.Trim()+"\n";
		if (line.tags.Length > 0) {
			foreach (string tag in line.tags) {
				Debug.Log(tag);
				string[] tokens = tag.Split(':');
				for (int i = 0; i < tokens.Length; i++) {tokens[i] = tokens[i].Trim();}
				if (tokens.Length > 1) {
					if (tokens[0] == "startChoice") {
						string[] derp = tokens[1].Split(','); for (int i = 0; i < derp.Length; i++) {derp[i] = derp[i].Trim();}
						if (derp.Length >= 1) {
							foreach (string s in derp) {
								CheckStartChoice(s);
							}
						}
						else {
							CheckStartChoice(tokens[1]);
						}
					}
					else if (tokens[0] == "endChoice") {
						int choiceIndex = -1;
						int.TryParse(tokens[1], out choiceIndex);
						if (choiceIndex != -1) {							
							for (int ii = 0; ii < choices.Count; ii++) {
								ChoiceObject _choice = choices[ii].GetComponent<ChoiceObject>();
								if (_choice.choiceIndex == choiceIndex) {
									Destroy(_choice.gameObject);
								}
							}
						}
					}
				}
			}
		}

		yield return null;
	}

	void CheckStartChoice(string token) {
		token = token.Trim();
		if (token == "all") {		
			StartCoroutine(LoadChoices(pendingChoices));
			pendingChoices.Clear();
		}
		else {
			int choiceIndex = -1;
			int.TryParse(token, out choiceIndex);
			if (choiceIndex != -1) {
				StartCoroutine(LoadChoices(GetChoices(choiceIndex)));
				for (int i = 0; i < pendingChoices.Count; i++) {
					if (pendingChoices[i].index == choiceIndex) {pendingChoices.RemoveAt(i); break;}
				}
				//pendingChoices.RemoveAt(choiceIndex);
			}
		}
	}

	IEnumerator WritePage() {
		lines = GetLines();
		List<Choice> _choices = StoryManager.story.currentChoices;
		yield return ManageTextBlock();

		GameObject go = lastTextBlock;
		Text t = go.GetComponentInChildren<Text>();
		RectTransform r = lastTextBlock.GetComponent<RectTransform>();
		CanvasGroup g = lastTextBlock.GetComponent<CanvasGroup>();
		lastTextBlock.GetComponent<FadeElement>().target = 1;

		t.text = "";
		if (pages.Count > 1) {t.text += "\n";}
		for (int i = 0; i < lines.Count; i++) {
			t.text += lines[i].text;
			if (lines[i].tags.Length > 0) {
				foreach (string tag in lines[i].tags) {
					Debug.Log(tag);
					string[] tokens = tag.Split(':');
					if (tokens.Length > 1) {
						if (tokens[0] == "startChoice") {
							int choiceIndex = -1;
							int.TryParse(tokens[1], out choiceIndex);
							if (choiceIndex != -1) {
								StartCoroutine(LoadChoices(GetChoices(choiceIndex)));
								_choices.RemoveAt(choiceIndex);
							}
						}
						else if (tokens[0] == "endChoice") {
							int choiceIndex = -1;
							int.TryParse(tokens[1], out choiceIndex);
							if (choiceIndex != -1) {							
								for (int ii = 0; ii < choices.Count; ii++) {
									ChoiceObject _choice = choices[ii].GetComponent<ChoiceObject>();
									if (_choice.choiceIndex == choiceIndex) {
										Destroy(_choice.gameObject);
									}
								}
							}
						}
					}
				}
			}
			t.text += "\n\n";
		}
		t.text = t.text.TrimEnd();

		StartCoroutine(LoadChoices(_choices));
	}

	string HandleLine(string line) {
		bool opening = true;
		line = line.Replace("--","—");
		line = line.Replace("...","…");
		line = line.Replace(" \""," “");
		line = line.Replace("\" ","” ");
		if (line.StartsWith("\"")) {line = line.Remove(0,1); line ="“"+line;}
		if (line.EndsWith("\"")) {line = line.Remove(line.Length-1,1); line = line+"”";}
		for (int i = 0; i < line.Length; i++) {
			if (line[i] == '_') {
				if (opening) {opening = false; line = line.Remove(i,1); line = line.Insert(i,"<i>"); i+=3;}
				else {opening = true; line = line.Remove(i,1); line = line.Insert(i,"</i>"); i+=4;}
			}
		}
		return line;
	}

	IEnumerator ChangeScene(string _scene) {
		yield return null;
	}

	IEnumerator LoadChoices(List<Choice> _choices) {
		if (_choices.Count > 0) {
			for (int i = 0; i < _choices.Count; ++i) {
				Choice choice = _choices[i];

				if (silentOptions.Contains(CleanText(choice.text))) {
					continue;
				}

				int ind = choice.index;
				GameObject choiceObj = GameObject.Instantiate(choicePrefab);
				choices.Add(choiceObj.transform);
				choiceObj.transform.SetParent(choiceAnchor);

				RectTransform rect = choiceObj.GetComponent<RectTransform>();
				rect.anchoredPosition = new Vector2(10*i,rect.anchoredPosition.y);

				UIFadeIn fade = choiceObj.GetComponent<UIFadeIn>();
				if (fade) { fade.delay = 0.7f + 0.5f * i; }

				Text _t = choiceObj.GetComponentInChildren<Text>();
				_t.text = ">  "+HandleLine(choice.text);

				Button b = choiceObj.GetComponent<Button>();
				if (b == null) {b = choiceObj.AddComponent<Button>();} 
				b.onClick.AddListener(() => StartCoroutine(SelectChoice(ind)));
				
				ChoiceObject _choiceObj = choiceObj.GetComponent<ChoiceObject>();
				if (_choiceObj == null) {_choiceObj = choiceObj.AddComponent<ChoiceObject>();} 
				_choiceObj.choiceIndex = ind;

				StartCoroutine(UIManager.instance.LoadChoice(choiceObj));
			}
		}
		Canvas.ForceUpdateCanvases();
		yield return null;
	}

	IEnumerator SelectChoice(int opt) {
		Debug.Log("Selected: " + opt);
		List<CanvasGroup> _yourChoice = new List<CanvasGroup>();
		for (int i = 0; i < choices.Count; i++) {
			if (choices[i]==null) {continue;}
			Destroy(choices[i].GetComponent<Button>());
			ChoiceObject cObj = choices[i].GetComponent<ChoiceObject>();
			if (!cObj || cObj.choiceIndex != opt) {choices[i].GetComponent<FadeElement>().target = 0;}
		}
		yield return new WaitForSeconds(0.3f);

		if (!multiblock) {
			foreach (GameObject page in pages) {
				if (page) {page.GetComponent<FadeElement>().target = 0;}
			}
		}
		yield return new WaitForSeconds(0.7f);

		Transform choiceTransform = null;
		foreach (Transform t in choices) {
			if (t==null) {continue;}
			ChoiceObject _c = t.GetComponent<ChoiceObject>();
			if (_c && _c.choiceIndex == opt) {choiceTransform = t; break;}
		}
		if (choiceTransform) {
			choiceTransform.GetComponent<FadeElement>().target = 0;
			yield return new WaitForSeconds(0.5f);
		}
		else {
			yield return new WaitForSeconds(0.5f);
		}
		string fullText = StoryManager.story.currentChoices[opt].text.Trim();
		StoryManager.story.ChooseChoiceIndex(opt);

		ClearChoices();
		lines.Clear();
		yield return StartCoroutine(Next());
		yield break;
	}

	IEnumerator MoveChoiceUp(Transform obj) {		
		obj.GetComponent<LayoutElement>().ignoreLayout = true;
		RectTransform rect = obj.GetComponent<RectTransform>();
		float speed = 3;
		float y = -10;
		if (!indent) {y = -36;}
		for (float i = 0; i < 1; i+=Time.fixedDeltaTime) { 
			rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition,new Vector2(rect.anchoredPosition.x,y),Time.fixedDeltaTime*speed);
			yield return new WaitForSeconds(Time.fixedDeltaTime);
		}
	}

	public static Rect RectTransformToScreenSpace(RectTransform transform) {
		Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
		float x = transform.position.x + transform.anchoredPosition.x;
		float y = Screen.height - transform.position.y - transform.anchoredPosition.y;

		return new Rect(x, y, size.x, size.y);
	}
}
