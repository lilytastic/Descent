using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ink.Runtime;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.IO;

public class Manager : MonoBehaviour {
	public SfxrSynth synth = new SfxrSynth();

	private Coroutine speakingRoutine = null;

	public AudioSource audioMain;
	public AudioMixer mainMixer;

	public static Vector3 positionOnLevelLoad = new Vector3();

	public static CameraControl mainCamera = null;
	public static Canvas mainCanvas = null;
	public static GameObject mainMenu = null;
	public static bool mainMenuOpen = false;

	public static State currentState = State.Dialogue;
	public enum State {
		Dialogue,
		Menu,
		Gameplay
	}

	static Manager _instance;
	private static object _lock = new object();
	static public bool isActive { get { return _instance != null; } }
	static public Manager instance {
		get {
			lock (_lock) {
				if (_instance == null) {
					Debug.Log("[Singleton] Creating Singleton: " + FindObjectsOfType(typeof(Manager)).Length.ToString());

					_instance = UnityEngine.Object.FindObjectOfType(typeof(Manager)) as Manager;

					if (FindObjectsOfType(typeof(Manager)).Length > 1) {
						Debug.LogError("[Singleton] Something went really wrong " +
							" - there should never be more than 1 singleton!" +
							" Reopening the scene might fix it.");
						//DontDestroyOnLoad(_instance.gameObject);
						return _instance;
					}

					if (_instance == null) {
						GameObject go = new GameObject("StoryManager Story");
						//DontDestroyOnLoad(go);
						_instance = go.AddComponent<Manager>();
						Debug.Log("[Singleton] An instance of " + typeof(Manager) +
							" is needed in the scene, so '" + go.name +
							"' was created with DontDestroyOnLoad.");
					}
					else {
						//DontDestroyOnLoad(_instance.gameObject);
						Debug.Log("[Singleton] Using instance already created: " +
							_instance.gameObject.name);
					}
				}
				return _instance;
			}
		}
	}

	public VoicePack voicePack = new VoicePack();

	public Color mainColor;

	public Language standardLanguage = new Language();
	
	public EventSystem eventSystem;

	public static List<Interactable> interactables = new List<Interactable>();

	public List<GameObject> interfaceElements = new List<GameObject>();

	public Entity Ananth = null;

	public Transform choiceAnchor;

	public GameObject particleEffect = null;

	public static Character player = null;

	public RectTransform dialogueView;
	public Text dialogueText;
	public Text speakerText;
	public Text sceneHeading;

	public Transform scriptView;
	public Text script;
	float viewWidth = 800 - (30 * 2) - 7;
	public Scrollbar scrollBar;

	public GameObject textPrefab;
	public GameObject choicePrefab;
	public GameObject dividerPrefab;

	public enum Emotion {
		Neutral,
		Happy,
		Angry,
		Sad,
		Mischevious,
		Flat
	}

	public Image screenFadeImage;
	public float screenFade = 0;

	public bool pause = false;
	public bool writing = false;
	public bool skip = false;
	public bool inProgress = false;

	public CanvasGroup dialogueUI;

	public List<string> linesToWrite = new List<string>();

	public LineFormat lastLine = LineFormat.Action;
	public string lastSpeaker = "";
	
	public Entity currentSpeaker = null;
	public Room currentRoom = null;

	string filename = "save1.txt";

	public Scene currentScene;

	public List<Transform> choices = new List<Transform>();

	public enum LineFormat {
		Action,
		Character,
		Dialogue,
		Parenthetical,
		Transition,
		SceneHeading,
		MissionHeading
	}

	void Start () {
		StartCoroutine(ChangeState(currentState,false));
		Debug.Log(StoryManager.instance.name);

		SceneManager.activeSceneChanged += OnSceneWasChanged;
		SceneManager.sceneLoaded += OnSceneWasLoaded;
		SceneManager.sceneUnloaded += OnSceneWasUnloaded;
		CameraControl cc = Camera.main.GetComponent<CameraControl>();
		if (cc) {
			mainCamera = cc;
		}
		else {
			mainCamera = Camera.main.gameObject.AddComponent<CameraControl>();
		}

		HandleLoad();
		StoryManager.inProgress = true;
		
		//StoryManager.bgm = (AudioClip)Resources.Load("Audio/Music/Faster Does It");
	}

	public IEnumerator ChangeState(State state, bool fade = true) {
		currentState = state;
		Debug.Log("Change to " + currentState.ToString());

		float steps = 100;
		if (dialogueText) { dialogueText.text = ""; }
		if (speakerText) { speakerText.text = ""; }
		if (currentState == State.Dialogue) {
			if (dialogueUI) {
				if (fade) {
					inProgress = true;
					dialogueUI.alpha = 0;
					for (int i = 0; i < steps; i++) {
						float lerp = i / steps;
						dialogueUI.alpha = lerp;
						yield return new WaitForSeconds(1 / steps);
					}
				}
				dialogueUI.alpha = 1;
			}
			inProgress = false;
			StartCoroutine(Next());
		}
		else {
			if (dialogueUI) {
				if (fade) {
					dialogueUI.alpha = 1;
					for (int i = 0; i < steps; i++) {
						float lerp = i / steps;
						dialogueUI.alpha = 1 - lerp;
						yield return new WaitForSeconds(1 / steps);
					}
				}
				dialogueUI.alpha = 0;
			}
		}

		yield return null;
	}
		
	void Update () {
		if (Input.GetKeyDown(KeyCode.Escape)) {
			if (mainMenuOpen) { StartCoroutine(instance.CloseMenu()); }
			else { StartCoroutine(instance.OpenMenu()); }
		}

		if (currentState == State.Dialogue) {
			bool clicked = Input.GetMouseButtonDown(0);
			if (clicked && EventSystem.current.IsPointerOverGameObject()) {
				GameObject woo = EventSystem.current.currentSelectedGameObject;
				if (woo) { clicked = false; }
			}
			if (clicked || Input.GetKey(KeyCode.LeftControl)) {
				//Debug.Log("Story can continue: " + StoryManager.story.canContinue.ToString());
				if (!writing && !inProgress) {
					//if (clicked) { PlaySound((AudioClip)Resources.Load("Audio/SFX/Interface/110429__soundbyter-com__www-soundbyter-com-selectsound"),0.2f); }
					StartCoroutine(Next());
				}
				else if (writing) {
					if (speakingRoutine != null) { StopCoroutine(speakingRoutine); }
					skip = true;
				}
			}
		}
		else {
			writing = false;
			inProgress = false;
		}

		if (screenFadeImage && screenFadeImage.color.a != screenFade) {
			screenFadeImage.color = new Color(0, 0, 0, screenFade);
		}

		float currentLowPass = 0; mainMixer.GetFloat("LowPass", out currentLowPass);
		mainMixer.SetFloat("LowPass", Mathf.Lerp(currentLowPass, mainMenuOpen ? 1750 : 22000, Time.fixedDeltaTime));

		float currentHighPass = 0; mainMixer.GetFloat("HighPass", out currentHighPass);
		mainMixer.SetFloat("HighPass", Mathf.Lerp(currentHighPass, mainMenuOpen ? 1000 : 0, Time.fixedDeltaTime));

		//lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency, mainMenuOpen ? 1750 : 9999, Time.fixedDeltaTime);
		//highPassFilter.cutoffFrequency = Mathf.Lerp(highPassFilter.cutoffFrequency, mainMenuOpen ? 1000 : 0, Time.fixedDeltaTime);
	}

	public IEnumerator OpenMenu() {
		if (mainMenuOpen || !mainMenu) { yield break; }
		mainMenuOpen = true;
		mainMenu.SetActive(true);

		CanvasGroup group = mainMenu.GetComponent<CanvasGroup>();
		if (group) {
			group.alpha = 0;
			float steps = 100;
			float fadeTime = 0.2f;
			for (int i = 0; i < steps; i++) {
				float lerp = (i / steps);
				group.alpha = lerp;
				yield return new WaitForSeconds(fadeTime / steps);
			}
			group.alpha = 1;
		}

		yield return null;
	}

	public IEnumerator CloseMenu() {
		if (!mainMenuOpen || !mainMenu) { yield break; }
		mainMenuOpen = false;

		CanvasGroup group = mainMenu.GetComponent<CanvasGroup>();
		if (group) {
			group.alpha = 1;
			float steps = 100;
			float fadeTime = 0.2f;
			for (int i = 0; i < steps; i++) {
				float lerp = (i / steps);
				group.alpha = 1 - lerp;
				yield return new WaitForSeconds(fadeTime / steps);
			}
			group.alpha = 0;
		}

		mainMenu.SetActive(false);
		yield return null;
	}


	public IEnumerator Next() {
		if (inProgress) { yield break; }
		pause = false;
		inProgress = true;

		//StartCoroutine(FlashInterface(new Color(255,255,255,0.4f)));

		//PlaySound((AudioClip)Resources.Load("Audio/SFX/198449__callum-sharp279__menu-scroll-selection-sound"));

		float beat = 0;
		foreach (string tag in StoryManager.story.currentTags) {
			//Debug.Log(tag);
			string[] tagTokens = tag.Split(':');
			if (tagTokens.Length > 1) {
				if (tagTokens[0] == "beat") {
					float.TryParse(tagTokens[1], out beat);
				}
			}
		}
		if (!Input.GetKey(KeyCode.LeftControl)) {yield return new WaitForSeconds(beat);}
		else {yield return new WaitForSeconds(0.1f);}

		if (linesToWrite.Count == 0) {
			while (!pause && StoryManager.story.canContinue) {
				string fullText = StoryManager.story.Continue().Trim();
				string[] textBroken = fullText.Split('/');

				foreach (string tag in StoryManager.story.currentTags) {
					Debug.Log(tag);
					string[] tagTokens = tag.Split(':');
					if (tagTokens.Length > 1) {
						if (tagTokens[0].Trim() == "atmosphere") {
							foreach (Atmosphere atmos in StoryManager.atmospheres) {
								if (atmos.name.ToLower() == tagTokens[1].Trim().ToLower()) {
									StartCoroutine(ChangeAtmosphere(atmos));
								}
							}
						}
						if (tagTokens[0].Trim() == "bgm") {
							AudioClip snd = (AudioClip)Resources.Load("Audio/Music/" + tagTokens[1].Trim());
							if (snd != StoryManager.bgm) { StoryManager.bgm = snd; }
							StoryManager.story.variablesState["bgm"] = tagTokens[1].Trim();
						}
						if (tagTokens[0].Trim() == "sfx") {
							AudioClip snd = (AudioClip)Resources.Load("Audio/SFX/Foley/" + tagTokens[1].Trim());
							StoryManager.PlaySound(snd);
						}
					}
					else if (tag.Trim() == "gameplay") {
						yield return StartCoroutine(ChangeState(State.Gameplay));
						inProgress = false;
						yield break;
					}
				}

				string textWithoutTokens = fullText.Replace(">", "").Replace(":", "").Replace("@", "").ToLower();
				if (textWithoutTokens == "fade in") {
					Debug.Log(textWithoutTokens);
					if (Input.GetKey(KeyCode.LeftControl)) {yield return StartCoroutine(SetFade(0, 1, 0.5f));}
					else {yield return StartCoroutine(SetFade(0, 1, 2f));}
				}
				if (textWithoutTokens == "fade out" || textWithoutTokens == "fade to black") {
					Debug.Log(textWithoutTokens);
					if (Input.GetKey(KeyCode.LeftControl)) { yield return StartCoroutine(SetFade(1, 0, 0.5f)); }
					else { yield return StartCoroutine(SetFade(1, 0, 2f)); }
				}
				else if (textWithoutTokens == "smash to black") {
					Debug.Log(textWithoutTokens);
					if (Input.GetKey(KeyCode.LeftControl)) { yield return StartCoroutine(SetFade(1, 0, 0.2f)); }
					else { yield return StartCoroutine(SetFade(1, 0, 0.2f)); }
				}

				LineFormat format = LineFormat.Action;

				format = GetFormat(fullText, lastLine);
				if (format == LineFormat.SceneHeading) {
					//currentRoom = fullText;
					yield return StartCoroutine(ChangeScene(fullText));
				}

				if (format != LineFormat.Dialogue && format != LineFormat.Parenthetical) {
					lastSpeaker = "";
					currentSpeaker = null;
				}

				if (format == LineFormat.Character) {
					lastSpeaker = fullText;
					string _name = fullText.Replace("(O.S.)", "").Replace("(COM)", "").Trim();
					for (var i = 0; i < StoryManager.entities.Length; i++) {
						if (StoryManager.entities[i].name.ToLower() == _name.ToLower()) { currentSpeaker = StoryManager.entities[i]; }
					}
				}
				else if (format == LineFormat.Action || format == LineFormat.Dialogue) {
					linesToWrite = textBroken.ToList();
					pause = true;
				}
				lastLine = format;
			}
		}

		if (linesToWrite.Count > 0) {
			yield return StartCoroutine(WriteLine(linesToWrite[0].Trim(), lastLine, lastSpeaker, 0, StoryManager.story.currentTags));
			linesToWrite.RemoveAt(0);
			//Debug.Log(linesToWrite.Count);

			if (linesToWrite.Count == 0) {
				LoadChoices();
			}
		}

		inProgress = false;

		yield return null;
	}
	
	IEnumerator WriteLine(string text, LineFormat format, string speaker = "", int succession = 0, List<string> tags = null) {
		if (!dialogueText) { yield break; }
		pause = true;
		writing = true;
		skip = false;

		Entity entity = null;

		string[] broken = text.Split(':');
		if (broken.Length > 1) { speaker = broken[0].Trim(); text = broken[1].Trim(); }

		speaker = speaker.Replace("(O.S.)", "").Replace("(COM)", "").Trim();
		for (var i = 0; i < StoryManager.entities.Length; i++) {
			if (StoryManager.entities[i].name.ToLower() == speaker.ToLower()) { entity = StoryManager.entities[i]; }
		}

		if (entity) {
			speaker = entity.nameDisplayed;
		}
		else if (speaker != "") {
			speaker = char.ToUpper(speaker[0]) + speaker.Substring(1).ToLower();
		}

		if (dialogueText) {
			dialogueText.color = Color.white;
			if (format == LineFormat.Action) {
				Color col = Color.white;
				ColorUtility.TryParseHtmlString("#C79F73FF", out col);
				dialogueText.color = col;
			}
		}
		if (speakerText) { speakerText.text = speaker; }

		if (format == LineFormat.Dialogue) {
			//if (!skip) { StoryManager.PlaySound((AudioClip)Resources.Load("Audio/SFX/Interface/179007__smartwentcody__book-page-turning-2"), 0.8f); }
			if (speakingRoutine != null) { StopCoroutine(speakingRoutine); }
			//speakingRoutine = StartCoroutine(Speak(Translate(text, standardLanguage)));
		}

		for (int i = 0; i < text.Length + 1; i++) {
			if (skip) { dialogueText.text = text; skip = false; break; }
			if (i < text.Length) { dialogueText.text = text.Substring(0, i) + "<color='#FFFFFF99'>" + text[i] + "</color><color='#FFFFFF00'>" + text.Substring(i + 1) + "</color>"; }
			else { dialogueText.text = text; }
			float delay = 0.012f;
			if (format == LineFormat.Dialogue) { delay = 0.02f; }
			yield return new WaitForSeconds(delay);
		}

		float beat = 0.15f;
		//yield return new WaitForSeconds(beat);

		writing = false;
	}

	void LoadChoices() {
		if (StoryManager.story.currentChoices.Count > 0) {
			for (int i = 0; i < StoryManager.story.currentChoices.Count; ++i) {
				Choice choice = StoryManager.story.currentChoices[i];
				//Debug.Log("Choice " + (i + 1) + ". " + choice.text);

				int ind = i;
				GameObject choiceObj = GameObject.Instantiate(choicePrefab);
				choices.Add(choiceObj.transform);
				//choiceObj.transform.SetParent(scriptView);
				choiceObj.transform.SetParent(choiceAnchor);
				Text _t = choiceObj.GetComponentInChildren<Text>();
				_t.text = choice.text;
				if (_t.text.StartsWith("\"") || _t.text.StartsWith("“")) {
					_t.text = "\""+_t.text.Remove(0, 1)+"\"";
				}
				else {
					_t.text = "[" + _t.text + "]";
				}
				//_t.text = "> " + _t.text;
				//_t.rectTransform.sizeDelta = new Vector2(viewWidth, 15);
				Button b = choiceObj.GetComponent<Button>();
				b.onClick.AddListener(() => StartCoroutine(SelectChoice(ind)));
			}
		}
	}

	IEnumerator SelectChoice(int opt) {
		string fullText = StoryManager.story.currentChoices[opt].text.Trim();
		StoryManager.story.ChooseChoiceIndex(opt);
		for (int i = 0; i < choices.Count; i++) {
			if (choices[i]) { Destroy(choices[i].gameObject); }
		}
		choices.Clear();

		/*
		GameObject divider = GameObject.Instantiate(dividerPrefab);
		divider.transform.SetParent(scriptView);
		*/

		if (fullText.StartsWith("\"") || fullText.StartsWith("“")) {
			//WriteLine("ANANTH", LineFormat.Character);
			//StartCoroutine(WriteLine(fullText.Remove(0, 1), LineFormat.Dialogue, "ANANTH"));
			currentSpeaker = Ananth;
			linesToWrite.Add("ANANTH: "+fullText.Remove(0,1));
		}

		yield return StartCoroutine(Next());
		//yield return StartCoroutine(ScrollToBottomGradual());
		yield break;
	}

	LineFormat GetFormat(string fullText, LineFormat lastLine = LineFormat.Action) {
		LineFormat format = LineFormat.Action;
		if (fullText.Length == 0) { return format; }

		string[] dialogueBroken = fullText.Split(':');

		string lowerText = fullText.ToLower();
		if (lowerText.StartsWith("int.") || lowerText.StartsWith("ext.")) { format = LineFormat.SceneHeading; }
		else if (fullText.StartsWith("[") && fullText.EndsWith("]")) { format = LineFormat.MissionHeading; }
		else if ((fullText.ToUpper() == fullText && fullText.EndsWith("TO:")) || fullText.StartsWith(">") || fullText == "FADE IN:") {
			format = LineFormat.Transition;
			if (fullText.StartsWith(">")) { fullText = fullText.Remove(0, 1).Trim(); }
		}
		else if ((fullText[0] == '@' || fullText.ToUpper() == fullText) && fullText!= "...") { format = LineFormat.Character; }
		else if ((lastLine == LineFormat.Character || lastLine == LineFormat.Dialogue) && fullText.StartsWith("(") && fullText.EndsWith(")")) { format = LineFormat.Parenthetical; }
		else if (dialogueBroken.Length > 1 || lastLine == LineFormat.Character || lastLine == LineFormat.Parenthetical) { format = LineFormat.Dialogue; }

		return format;
	}

	IEnumerator ChangeScene(string room, bool fadeIn = true, Vector3 pos = new Vector3()) {
		if (sceneHeading) { sceneHeading.text = room; }
		string sceneName = room.Replace("INT.", "").Replace("EXT.", "").Trim();
		Debug.Log(sceneName);
		StoryManager.currentScene = room;
		
		string[] sceneTokens = sceneName.Split(new char[] { '-', ',' });
		foreach (string token in sceneTokens) {
			foreach (Atmosphere atmos in StoryManager.atmospheres) {
				if (atmos.name.ToLower() == token.ToLower().Trim()) {
					StartCoroutine(ChangeAtmosphere(atmos));
					break;
				}
			}
		}

		string technicalName = sceneName.ToLower().Trim();
		if (technicalName != "") {
			string _scene = StoryManager.GetScene(technicalName);
			if (_scene != "") {
				if (currentScene.name == null || currentScene.name != SceneManager.GetSceneByName(_scene).name) {
					if (screenFade < 0.5f) { yield return SetFade(1, 1f); }

					if (currentScene.name != null) {
						SceneManager.UnloadSceneAsync(currentScene);
					}
					SceneManager.LoadScene(_scene, LoadSceneMode.Additive);
					currentScene = SceneManager.GetSceneByName(_scene);
					positionOnLevelLoad = pos;

					yield return new WaitForSeconds(0.5f);
					yield return SetFade(0, 1);
				}
				else {
					Debug.Log("Attempting to reload " + technicalName);
				}
			}
		}

		AudioClip snd = null;
		foreach (string tag in StoryManager.story.currentTags) {
			Debug.Log(tag);
			string[] tagTokens = tag.Split(':');
			if (tagTokens.Length > 0) {
				if (tagTokens[0].Trim() == "atmosphere") {
					foreach (Atmosphere atmos in StoryManager.atmospheres) {
						if (atmos.name.ToLower() == tagTokens[1].Trim().ToLower()) {
							StartCoroutine(ChangeAtmosphere(atmos));
						}
					}
				}
				if (tagTokens[0].Trim() == "bgm") {
					snd = (AudioClip)Resources.Load("Audio/Music/" + tagTokens[1].Trim());
				}
			}
		}

		if (snd != StoryManager.bgm) { StartCoroutine(StoryManager.instance.ChangeBGM(snd)); }
		StoryManager.story.variablesState["room"] = room;

		yield break;
	}

	void OnSceneWasChanged(Scene previousScene, Scene nextScene) {
	}
	void OnSceneWasLoaded(Scene _scene, LoadSceneMode mode) {
		Camera[] cameras = GameObject.FindObjectsOfType<Camera>();
		if (cameras.Length > 1) {
			int camerasDestroyed = 0;
			for (int i = 0; i < cameras.Length; i++) {
				CameraControl cc = cameras[i].GetComponent<CameraControl>();
				if (!cc && cameras[i] != mainCamera && !(i == cameras.Length - 1 && camerasDestroyed == cameras.Length - 1)) {
					camerasDestroyed++;
					Destroy(cameras[i].gameObject);
				}
			}
		}

		mainCanvas = GameObject.FindObjectOfType<Canvas>();
		Debug.Log(mainCanvas);

		if (!mainMenu) {
			mainMenu = GameObject.Find("Main Menu");
			if (!mainMenu) { mainMenu = GameObject.Instantiate((GameObject)Resources.Load("Prefabs/Utility/Main Menu")); }
			mainMenu.transform.SetParent(mainCanvas.transform);
			RectTransform _rect = mainMenu.GetComponent<RectTransform>();
			_rect.sizeDelta = new Vector2(0, 0);
			_rect.anchoredPosition = new Vector2(0, 0);
			mainMenu.SetActive(mainMenuOpen);
		}

		GameObject _player = GameObject.FindGameObjectWithTag("Player");
		if (!_player) { _player = GameObject.Instantiate((GameObject)Resources.Load("Prefabs/Player")); }

		if (positionOnLevelLoad != new Vector3()) {
			_player.transform.position = positionOnLevelLoad;
			positionOnLevelLoad = new Vector3();
		}
		else {
			PositionMarker[] markers = GameObject.FindObjectsOfType<PositionMarker>();
			_player.transform.position = new Vector3();
			for (int i = 0; i < markers.Length; i++) {
				if (markers[i].defaultEntry) { _player.transform.position = markers[i].transform.position; }
			}
		}

		Character c = _player.GetComponent<Character>();
		if (c) {
			Manager.player = c;
			c.movement = _player.transform.position;
		}
		mainCamera.relativeTo = _player.transform;

		foreach (GameObject g in GameObject.FindGameObjectsWithTag("Invisible")) {
			MeshRenderer mr = g.GetComponent<MeshRenderer>();
			if (mr) { mr.enabled = false; }
		}

	}
	void OnSceneWasUnloaded(Scene _scene) {

	}

	IEnumerator ChangeAtmosphere(Atmosphere atmos) {
		if (particleEffect) {
			ParticleSystem ps = particleEffect.GetComponent<ParticleSystem>();
			if (ps) {
				ps.Stop();
				Destroy(particleEffect, ps.main.duration);
			}
			else { Destroy(particleEffect); }
		}
		/*
		particleEffect = null;
		if (atmos.particles) {
			particleEffect = GameObject.Instantiate(atmos.particles);
			particleEffect.transform.SetParent(transform);
			particleEffect.transform.localPosition = new Vector3();
			particleEffect.transform.rotation = new Quaternion();
		}
		*/
		Color priorColor = Camera.main.backgroundColor;
		float steps = 100;
		float length = 1.5f;
		for (int i = 1; i < steps; i++) {
			float lerp = i / steps;
			Camera.main.backgroundColor = Color.Lerp(priorColor,atmos.backgroundColor,lerp);
			yield return new WaitForSeconds(length/steps);
		}
		Camera.main.backgroundColor = atmos.backgroundColor;
		yield break;
	}

	void HandleLoad() {
		Clear();

		Vector3 pos = new Vector3();
		if (StoryManager.lastSave != null) { pos = new Vector3(StoryManager.lastSave.posX, StoryManager.lastSave.posY, StoryManager.lastSave.posZ); }
		StartCoroutine(ChangeScene(StoryManager.story.variablesState["room"].ToString(),true,pos));
		object stream = null;
		StoryManager.story.state.jsonToken.TryGetValue("outputStream", out stream);
		if (stream != null) {
			List<object> streamObj = (List<object>)stream;
			if (streamObj.Count > 0) {
				for (int i = 0; i < streamObj.Count; i++) {
					// Find the first actual string.
					if (streamObj[i] is Dictionary<string,object>) {
						Dictionary<string,object> tags = streamObj[i] as Dictionary<string,object>;
						if ((string)tags["#"] == "gameplay") { StartCoroutine(ChangeState(State.Gameplay, false));  break; }
					}
					else if (streamObj[i] is string) {
						StartCoroutine(ChangeState(State.Dialogue, false));
						StartCoroutine(WriteLine(streamObj[i].ToString().Substring(1), LineFormat.Dialogue));
						break;
					}
				}
			}
			else {
				StartCoroutine(Next());
			}
		}
		StartCoroutine(StoryManager.instance.ChangeBGM((AudioClip)Resources.Load("Audio/Music/"+StoryManager.story.variablesState["bgm"])));
		StartCoroutine(Manager.instance.CloseMenu());
	}

	void Clear() {
		if (scriptView) {
			for (int i = 0; i < scriptView.childCount; i++) {
				Destroy(scriptView.GetChild(i).gameObject);
			}
			Debug.Log(scriptView.childCount);
		}
	}

	public void ScrollToBottom() {
		scrollBar.value = 0;
	}

	IEnumerator ScrollToBottomGradual(GameObject divider) {
		yield return new WaitForSeconds(0.1f);
		float startingScroll = scrollBar.value;
		for (int i = 0; i < 100; i++) {
			if (scrollBar.value < 0.01f) { break; }
			scrollBar.value = Mathf.Lerp(scrollBar.value, 0, 0.1f);
			yield return new WaitForSeconds(0.03f);
		}

		yield break;
	}

	Word[] Translate(string text, Language language) {
		string[] words = text.Split(' ');
		List<Word> translated = new List<Word>();
		string newSentence = "";
		foreach (string s in words) {
			List<Phoneme> phonemes = new List<Phoneme>();
			int phonemeCount = 1; // +Mathf.RoundToInt(s.Length/2 * Random.Range(-0.95f,1.1f));
			if (phonemeCount < 1) { phonemeCount = 1; }
			int vowelCount = 0;
			Phoneme lastPhoneme = null;
			for (int i = 0; i < phonemeCount; i++) {
				Phoneme p = null;
				bool getVowel = false;
				if (lastPhoneme != null && !lastPhoneme.vowel) { getVowel = true; }
				if (i == phonemeCount-1 && vowelCount == 0) { getVowel = true; }
				if (getVowel) {
					p = language.GetVowel();
					vowelCount++;
				}
				else {
					p = language.GetConsonant();
				}
				lastPhoneme = p;
				phonemes.Add(p);
			}
			Phoneme[] arr = phonemes.ToArray();
			Word word = new Word(arr, language.PrintWord(arr));
			if (s.EndsWith(",")) { word.endsWith = ","; Debug.Log(s); }
			if (s.EndsWith(".")) { word.endsWith = "."; }
			if (s.EndsWith("!")) { word.endsWith = "!"; }
			if (s.EndsWith("?")) { word.endsWith = "?"; }
			if (s.EndsWith(";")) { word.endsWith = ";"; }
			translated.Add(word);
		}
		foreach (Word w in translated) {
			newSentence += w.printed + " ";
		}
		Debug.Log(newSentence);
		return translated.ToArray();
	}

	IEnumerator Speak(Word[] sentence, Emotion direction = Emotion.Neutral) {
		float variation = 0.02f; // 0.2f
		float freqStandard = 0.3f; // 0.2662f; //0.5096437f;
		float volumeStandard = 0.1f;
		float speechVariation = 0.5f;
		float speed = 1f;

		switch (direction) {
			case Emotion.Flat:
				speechVariation = 0.2f;
				break;
			case Emotion.Angry:
				speechVariation = 0.8f;
				variation = 0.05f;
				freqStandard = 0.32f;
				volumeStandard = 0.15f;
				speed = 1.2f;
				break;
			case Emotion.Mischevious:
				speechVariation = 0.6f;
				freqStandard = 0.29f;
				speed = 0.9f;
				break;
			default:
				break;
		}

		for (int ii = 0; ii < sentence.Length; ii++)  {
			Word word = sentence[ii];
			float wordEmphasis = Random.Range(0f,1f)*speechVariation;

			if (word.endsWith == ".") {
				wordEmphasis = speechVariation;
			}

			for (int i = 0; i < word.phonemes.Length; i++) {
				Phoneme phoneme = word.phonemes[i];
				string settings = "";
				bool alternating = (i % 2 == 0);
				float volume = volumeStandard * Random.Range(0.97f, 1.03f) + wordEmphasis*0.2f;
				
				if (ii == 0) { volume *= 1.1f; }
				float freq = freqStandard;
				freq += -0.05f+wordEmphasis*0.1f;
				freq *= 1 - ((ii + 1)/sentence.Length) * 0.16f;
				if (alternating) { freq *= 0.96f; volume *= 0.96f; }

				float slideVariation = 0.01f;
				float slide = -0.136f + Random.Range(-slideVariation*0.5f, slideVariation*0.5f);

				float sustainVariation = 0.04f;
				float sustainTime = 0.1123f + Random.Range(-sustainVariation * 0.5f, sustainVariation * 0.5f);

				if (word.endsWith == ".") {
					if (direction == Emotion.Mischevious) { freq += 0.05f; volume -= 0.03f; sustainTime += 0.05f; }
					if (direction == Emotion.Angry) { volume += 0.07f; }
				}

				float decay = 0.18f;
				switch (phoneme.sound) {
					default:
						settings = "8,.5,.095,"+sustainTime.ToString()+",,"+decay.ToString()+",.3,"+freq.ToString()+ ",," + slide.ToString() + ",-.109,,,,,,,,,,,,,.082,,.58,,.291,.264,,,-.373";
						// "8,.5,,.1123,,.1423,.3,.2662,,-.114,-.109,,,,,,,,,,,,,.082,,.58,,.291,.264,,,-.373";
						break; 
					//"1,.5,,.1123,,.1423,.3,.2662,,,,,,,,,,,,,,,,,,1,,,.1,,,"; break; // "0,.5,,.1284,,.1859,.3,.3104,,,,,,,,,,,,,.2266,,,,,1,,,.1,,,"; break;
				}
				if (settings != "") { synth.parameters.SetSettingsString(settings); }
				synth.parameters.masterVolume = volume;
				synth.parameters.Mutate(variation);
				synth.Play();
				if (i == word.phonemes.Length - 1) { yield return new WaitForSeconds((sustainTime - 0.02f)/speed); }
			}
			float beat = 0.04f;
			if (word.endsWith == "," || word.endsWith == ";") { beat += 0.1f; }
			if (word.endsWith == ".") { beat += 0.3f; }
			yield return new WaitForSeconds(beat / speed);
		}
		yield return null;
	}

	IEnumerator FlashInterface(Color col) {
		Color prevCol = mainColor;
		float steps = 50;
		float length = 0.04f;
		for (int i = 1; i < steps; i++) {
			float lerp = i / steps;
			mainColor = Color.Lerp(mainColor, col, lerp);
			UpdateInterface();
			yield return new WaitForSeconds(length / steps);
		}
		for (int i = 1; i < steps; i++) {
			float lerp = i / steps;
			mainColor = Color.Lerp(mainColor, prevCol, lerp);
			UpdateInterface();
			yield return new WaitForSeconds(length / steps);
		}
		UpdateInterface();
		yield return null;
	}

	void UpdateInterface() {
		foreach (GameObject g in interfaceElements) {
			Image im = g.GetComponent<Image>();
			if (im) {
				im.color = mainColor;
			}
		}
	}

	IEnumerator SetFade(float val, float time) {
		float steps = 50;
		for (int i = 1; i < steps; i++) {
			float lerp = i / steps;
			screenFade = Mathf.Lerp(screenFade, val, lerp);
			yield return new WaitForSeconds(time / steps);
		}
		screenFade = val;
		yield return null;
	}
	IEnumerator SetFade(float val, float from, float time) {
		float steps = 50;
		for (int i = 1; i < steps; i++) {
			float lerp = i / steps;
			screenFade = Mathf.Lerp(from, val, lerp);
			yield return new WaitForSeconds(time / steps);
		}
		screenFade = val;
		yield return null;
	}

}
