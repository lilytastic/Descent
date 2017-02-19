using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using Ink.Runtime;

public class NovelManager : MonoBehaviour {

	bool inProgress = false;
	bool pause = false;

	List<string> linesToWrite = new List<string>();

	private string[] silentOptions = new string[] {"...","say nothing","remain silent","none"};

	public AudioClip clipOnNext;
	public AudioClip clipOnSelect;

	public AudioMixer mainMixer;

	StoryManager.LineFormat lastLine = StoryManager.LineFormat.Action;
	string lastSpeaker = "";
	Entity currentSpeaker = null;

	public ChoiceObject lastChoice = null;
	public GameObject lastTextBlock = null;
	public List<Transform> choices = new List<Transform>();
	public List<Choice> pendingChoices = new List<Choice>();
	public List<Choice> lastChoices = new List<Choice>();

	public bool multiblock = true;
	public bool indent = false;
	public bool continueMaximally = false;
	public bool ascendingParagraphs = false;
	public bool storeChoices = false;
	public float scrollSpeed = 3; // -1 for insta-scroll

	public SaveFile currentSave;

	public ScrollRect scroll;
	public RectTransform scrollTransform;
	public RectTransform scriptView;	
	
	public RectTransform canContinueIndicator;

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
	
	void Start () {
		//StartCoroutine(WritePage());
	}
	
	float timeHeldMouseButton = 0;
	void Update () {
		switch (GameManager.instance.gameScreen.state) {
			case GameManager.ScreenState.Story:
				if (Input.GetMouseButtonUp(0) && timeHeldMouseButton < 0.2f && !GameManager.instance.stopWriting) {
					StartCoroutine(Next(true));
				}
				if (Input.GetMouseButton(0)) {timeHeldMouseButton+=Time.deltaTime;}
				else {timeHeldMouseButton=0;}
				break;
			default:
				break;
		}
	}

	public void HandleLoad() {
		ClearText();
		ClearChoices();
		GameManager.instance.mainScreenManager.ChangeState("Game");
		GameManager.instance.gameScreen.ChangeState("Story");
		StoryLine _line = new StoryLine();		
		_line.text = ((List<object>)StoryManager.story.state.jsonToken["outputStream"])[0].ToString().Substring(1);
		Debug.Log(_line.text);
		StartCoroutine(WriteLine(_line));
	}

	public static AudioSource PlaySound(AudioClip clip, float volume = 1, float pitch = 1) {
		if (clip == null) {return null;}
		GameObject obj = new GameObject();
		AudioSource src = obj.AddComponent<AudioSource>();
		src.clip = clip;
		src.volume = volume;
		src.pitch = pitch;
		src.outputAudioMixerGroup = instance.mainMixer.FindMatchingGroups("Master")[0];
		src.Play();
		Destroy(obj, clip.length);
		return src;
	}

	public void ClearText() {
		for (int i = 0; i < textBlockAnchor.transform.childCount; i++) {
			Transform t = textBlockAnchor.transform.GetChild(i);
			if (t != canContinueIndicator && t != choiceAnchor) {Destroy(t.gameObject);}
		}
		for (int i = 0; i < pages.Count; i++) {
			if (pages[i].gameObject) {Destroy(pages[i].gameObject);}
		}
		pages.Clear();
	}
	public void ClearChoices(List<Choice> exclude = null) {
		List<string> excludeStrings = new List<string>();
		//if (exclude != null) {foreach (Choice c in exclude) {excludeStrings.Add(c.text);}}
		for (int i = 0; i < choiceAnchor.transform.childCount; i++) {
			Transform t = choiceAnchor.transform.GetChild(i);
			ChoiceObject cObj = t.GetComponent<ChoiceObject>();
			if (!cObj || !excludeStrings.Contains(cObj.text)) {
				Destroy(t.gameObject);
			}
		}
		/*
		for (int i = 0; i < choices.Count; i++) {
			if (choices[i] == null) {continue;}
			if (choices[i].gameObject) {Destroy(choices[i].gameObject);}
		}
		*/
		choices.Clear(); //RemoveAll(x => !excludeObjs.Contains(x));
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
			Debug.Log(StoryManager.story.currentChoices.Count);
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

	public IEnumerator Next(bool userPrompted = false) {
		//if (GameManager.instance.stopWriting) {yield break;}
		GameObject duh = EventSystem.current.currentSelectedGameObject;
		//Debug.Log(duh ? duh.name : "No object");
		if (userPrompted && duh && duh.GetComponent<Button>()) {yield return null;}
		else {
			if (storeChoices) {
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
							//Debug.Log(":)");
							StartCoroutine(SelectChoice(c.index,true));
						}
					}
				}
				//Debug.Log(lines.Count);
				if (lines.Count > 0) {
					if (userPrompted) {PlaySound(instance.clipOnNext);}
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
				else {}
			}
			else {
				if (StoryManager.story.canContinue) {
					if (userPrompted) {PlaySound(instance.clipOnNext);}
					StoryLine line = new StoryLine();
					line.text = HandleLine(StoryManager.story.Continue());
					line.tags = StoryManager.story.currentTags.ToArray();
					//Debug.Log("Writing a line now");
					if (GameManager.instance.latestDiceRoll > -1) {
						GameManager.instance.stopWriting = true;
						int roll = GameManager.instance.latestDiceRoll;
						int required = GameManager.instance.latestDiceRollRequirement;
						Debug.Log("There was a dice roll of " + roll + " against " + required + ", which is a " + ((roll >= required) ? "success" : "failure"));
						GameManager.instance.latestDiceRoll = -1;
						GameManager.instance.latestDiceRollRequirement = -1;
						GameManager.instance.PlaySound(GameManager.instance.soundDiceRoll,1,Random.Range(0.9f,1.1f));
						yield return new WaitForSeconds(1.1f);
						GameManager.instance.stopWriting = false;
					}
					StartCoroutine(WriteLine(line));
					FadeElement _fade = choiceAnchor.GetComponent<FadeElement>();
					if (!StoryManager.story.canContinue) {
						//ClearChoices(StoryManager.story.currentChoices);
						StartCoroutine(LoadChoices(StoryManager.story.currentChoices));
					}
					else {
						//ClearChoices();
						if (_fade) {_fade.target = 0;}
					}
					
					if (canContinueIndicator) {
						FadeElement __fade = canContinueIndicator.GetComponent<FadeElement>();
						if (__fade) {__fade.target = (CanContinue() ? 1 : 0);} else {canContinueIndicator.gameObject.SetActive(CanContinue());}
					}
				}
				else {					
					bool woo = false;
					foreach (Choice c in StoryManager.story.currentChoices) {
						if (c.text.Trim() == "NONE") {
							StartCoroutine(SelectChoice(c.index,true));
							woo = true;
							yield return null;
						}
					}
					if (!woo) {
						foreach (Choice c in StoryManager.story.currentChoices) {
							string cleanText = CleanText(c.text);
							//Debug.Log(cleanText);
							if (silentOptions.Contains(cleanText)) {
								StartCoroutine(SelectChoice(c.index,true));
							}
						}
					}
				}
			}
		}
		yield return null;
	}

	bool CanContinue() {
		bool canIContinue = StoryManager.story.canContinue;
		if (!canIContinue) {
			foreach (Choice c in StoryManager.story.currentChoices) {
				if (silentOptions.Contains(c.text.Trim().ToLower())) {canIContinue = true; break;}
			}
		}
		return canIContinue;
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

		UIManager.instance.CapPages();
		go.name = Random.Range(0,9999).ToString();
		Canvas.ForceUpdateCanvases();
		RectTransform rect = go.GetComponent<RectTransform>();
		float pos = rect.anchoredPosition.y+100;
		float offset = pos/scriptView.sizeDelta.y;
		//Debug.Log(offset);
		//Debug.Log(offset);

		if (ascendingParagraphs) {rect.SetAsFirstSibling();}
		else {scroll.verticalNormalizedPosition = offset;}

		if (canContinueIndicator) {canContinueIndicator.SetAsLastSibling();}
		//StartCoroutine(ScrollTo(go));
	}
	IEnumerator ScrollTo(float offset) {
		for (float i = 0; i < 1000; i+=1) {
			float dist = scroll.verticalNormalizedPosition-offset;
			Debug.Log(dist);
			if (scroll.verticalNormalizedPosition == offset || dist < 0.00001f) {
				scroll.verticalNormalizedPosition = offset;
				break;
			}
			
			if (scroll.verticalNormalizedPosition > offset) {
				scroll.verticalNormalizedPosition -= Time.fixedDeltaTime;
				if (scroll.verticalNormalizedPosition < offset) {scroll.verticalNormalizedPosition = offset;}
			}
			else if (scroll.verticalNormalizedPosition < offset) {
				scroll.verticalNormalizedPosition += Time.fixedDeltaTime;
				if (scroll.verticalNormalizedPosition > offset) {scroll.verticalNormalizedPosition = offset;}
			}
			
			//scroll.verticalNormalizedPosition = offset;
			yield return new WaitForSeconds(Time.fixedDeltaTime);
		}
		yield return null;
	}

	IEnumerator ScrollTo(GameObject obj) {
		RectTransform rect = obj.GetComponent<RectTransform>();
		Debug.Log(rect.name + ": size is " + rect.sizeDelta);
		float height = rect.sizeDelta.y;
		float speed = 0.75f;
		for (float i = 0; i < 1000; i+=1) {
			if (i == 999) {Debug.Log("VWROKSF");}
			float offset = (-rect.anchoredPosition.y)/(scriptView.sizeDelta.y-rect.sizeDelta.y);
			offset = Mathf.Clamp01( 1 - offset - (offset/scriptView.sizeDelta.y) );
			Debug.Log(offset);
						
			//scroll.verticalNormalizedPosition = offset;

			float dist = scroll.verticalNormalizedPosition-offset;
			scroll.verticalNormalizedPosition = offset;

			//Debug.Log(dist);
			if (scroll.verticalNormalizedPosition == offset || dist < 0.00001f) {
				scroll.verticalNormalizedPosition = offset;
				break;
			}
			
			if (scroll.verticalNormalizedPosition > offset) {
				scroll.verticalNormalizedPosition -= Time.fixedDeltaTime*speed;
				if (scroll.verticalNormalizedPosition < offset) {scroll.verticalNormalizedPosition = offset;}
			}
			else if (scroll.verticalNormalizedPosition < offset) {
				scroll.verticalNormalizedPosition += Time.fixedDeltaTime*speed;
				if (scroll.verticalNormalizedPosition > offset) {scroll.verticalNormalizedPosition = offset;}
			}
			
			//scroll.verticalNormalizedPosition = offset;
			yield return new WaitForSeconds(0.03f);
		}
		yield return null;
	}

	IEnumerator WriteLine(StoryLine line) {
		List<Choice> _choices = StoryManager.story.currentChoices;
		yield return ManageTextBlock();
		
		GameObject go = lastTextBlock;
		Text t = go.GetComponentInChildren<Text>();
		RectTransform r = lastTextBlock.GetComponent<RectTransform>();
		CanvasGroup g = lastTextBlock.GetComponent<CanvasGroup>();
		lastTextBlock.GetComponent<FadeElement>().target = 1;
		//r.name = line.text.Trim().Substring(0,30);

		t.text = line.text.Trim()+"\n";
		if (line.tags.Length > 0) {
			foreach (string tag in line.tags) {
				Debug.Log(tag);
				string[] tokens = tag.Split(':');
				for (int i = 0; i < tokens.Length; i++) {tokens[i] = tokens[i].Trim();}
				if (tokens.Length > 1) {
					switch (tokens[0]) {
						case "startChoice":
							if (!storeChoices) {break;}
							string[] derp = tokens[1].Split(','); for (int i = 0; i < derp.Length; i++) {derp[i] = derp[i].Trim();}
							if (derp.Length >= 1) {
								foreach (string s in derp) {
									CheckStartChoice(s);
								}
							}
							else {
								CheckStartChoice(tokens[1]);
							}
							break;
						case "endChoice":
							if (!storeChoices) {break;}
							int choiceIndex = -1;
							int.TryParse(tokens[1], out choiceIndex);
							if (choiceIndex != -1) {							
								for (int ii = 0; ii < choices.Count; ii++) {
									ChoiceObject _choice = choices[ii].GetComponent<ChoiceObject>();
									if (_choice.choiceIndex == choiceIndex) {
										StartCoroutine(RemoveChoice(_choice.gameObject));
									}
								}
							}
							break;
						default: 
							break;
					}
				}
			}
		}

		Canvas.ForceUpdateCanvases();
		
		StopCoroutine("ScrollTo");
		//StartCoroutine(ScrollTo(go));
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

	IEnumerator RemoveChoice(GameObject _choice) {
		Destroy(_choice.GetComponent<Button>());
		_choice.GetComponent<FadeElement>().target = 0;
		yield return new WaitForSeconds(1.2f);
		Destroy(_choice);		
		yield return null;
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

	public static string ChangeScene(string _scene) {
		Debug.Log(_scene);
		
		instance.StartCoroutine(instance.SceneChange(_scene));
		return "";
	}

	IEnumerator SceneChange(string _scene) {
		yield return new WaitForSeconds(1f);
		instance.ClearText();
		//StartCoroutine(Next());
		yield return null;
	}

	IEnumerator LoadChoices(List<Choice> _choices) {
		//yield return new WaitForSeconds(0.1f);
		if (_choices.Count > 0) {
			ChoiceObject[] prevChoices = GameObject.FindObjectsOfType<ChoiceObject>();
			Debug.Log(prevChoices.Length);
			for (int i = 0; i < _choices.Count; ++i) {
				Choice choice = _choices[i];

				if (silentOptions.Contains(CleanText(choice.text))) {
					continue;
				}

				int ind = choice.index;
				GameObject choiceObj = null;
				/*
				foreach (ChoiceObject c in prevChoices) {
					string _ss = choice.text.Trim();
					Debug.Log(c.text + " != " + _ss);
					if (c.text.Trim() == _ss) {
						Debug.Log(":D");
						choiceObj = c.gameObject;
						FadeElement __fade = c.GetComponent<FadeElement>();
						if (__fade) {__fade.target = 1;}
						Debug.Log(c.gameObject);
						//Destroy(choiceObj.GetComponent<Button>());
						//choiceObj.AddComponent<Button>();
						if (!choices.Contains(c.transform)) {choices.Add(c.transform);}
					}
					else {						
						//Debug.Log(cc.text + "!=" + _ss);
					}
				}
				*/				
				if (choiceObj == null) {
					choiceObj = GameObject.Instantiate(choicePrefab);
					choiceObj.name = Random.Range(0,99999).ToString();
					choices.Add(choiceObj.transform);
					choiceObj.transform.SetParent(choiceAnchor);
				}

				RectTransform rect = choiceObj.GetComponent<RectTransform>();
				rect.anchoredPosition = new Vector2(10*i,rect.anchoredPosition.y);

				UIFadeIn fade = choiceObj.GetComponent<UIFadeIn>();
				if (fade) { fade.delay = 0.7f + 0.5f * i; }

				Text _t = choiceObj.GetComponentInChildren<Text>();
				_t.text = HandleLine(choice.text);

				Button b = choiceObj.GetComponent<Button>();
				if (b == null) {b = choiceObj.AddComponent<Button>();} 
				else {b.enabled = true;}
				b.onClick.AddListener(() => StartCoroutine(SelectChoice(ind)));
				
				ChoiceObject _choiceObj = choiceObj.GetComponent<ChoiceObject>();
				if (_choiceObj == null) {_choiceObj = choiceObj.AddComponent<ChoiceObject>();} 
				_choiceObj.choiceIndex = ind;
				_choiceObj.text = choice.text;

				StartCoroutine(UIManager.instance.LoadChoice(choiceObj));
			}
		}
		lastChoices.Clear();
		FadeElement _fade = choiceAnchor.GetComponent<FadeElement>();
		if (_fade) {
			if (choices.Count > 0) {_fade.target = 1;}
			else {_fade.target = 0;}
		}
		//if (canContinueIndicator) {canContinueIndicator.SetAsLastSibling();}
		Canvas.ForceUpdateCanvases();
		yield return null;
	}

	IEnumerator SelectChoice(int opt, bool silent = false) {
		GameManager.instance.stopWriting = true;
		//Debug.Log("Selected: " + opt);
		List<CanvasGroup> _yourChoice = new List<CanvasGroup>();

		lastChoices = StoryManager.story.currentChoices;

		if (!silent) {PlaySound(clipOnSelect);}

		for (int i = 0; i < choices.Count; i++) {
			if (choices[i]==null) {continue;}
			choices[i].GetComponent<Button>().enabled = false;
			ChoiceObject cObj = choices[i].GetComponent<ChoiceObject>();
			if (!cObj || cObj.choiceIndex != opt) {choices[i].GetComponent<FadeElement>().target = 0;}
		}
		if (!silent) {yield return new WaitForSeconds(0.3f);}

		if (!multiblock) {
			foreach (GameObject page in pages) {
				if (page) {page.GetComponent<FadeElement>().target = 0;}
			}
		}
		if (!silent) {yield return new WaitForSeconds(0.7f);}

		Transform choiceTransform = null;
		foreach (Transform t in choices) {
			if (t==null) {continue;}
			ChoiceObject _c = t.GetComponent<ChoiceObject>();
			if (_c && _c.choiceIndex == opt) {choiceTransform = t; break;}
		}
		if (choiceTransform) {
			choiceTransform.GetComponent<FadeElement>().target = 0;
			if (!silent) {yield return new WaitForSeconds(0.5f);}
		}
		else {
			if (!silent) {yield return new WaitForSeconds(0.5f);}
		}
		//string fullText = StoryManager.story.currentChoices[opt].text.Trim();
		StoryManager.story.ChooseChoiceIndex(opt);

		ClearChoices();
		lines.Clear();
		GameManager.instance.stopWriting = false;
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
