using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ink.Runtime;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.IO;

public class Manager : MonoBehaviour {
	public SfxrSynth synth = new SfxrSynth();

	private Coroutine speakingRoutine = null;

	public AudioSource audioMain;

	public VoicePack voicePack = new VoicePack();

	public Color mainColor;

	public Language standardLanguage = new Language();

	public Canvas mainCanvas;
	public EventSystem eventSystem;

	public static List<Interactable> interactables = new List<Interactable>();

	public List<GameObject> interfaceElements = new List<GameObject>();

	public Entity Ananth = null;

	public Transform choiceAnchor;

	public GameObject particleEffect = null;

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

	Core.State currentState = Core.State.Dialogue;

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
		currentState = Core.currentState;
		Debug.Log(Core.instance.name);
		HandleLoad();
		Core.inProgress = true;
		//Core.bgm = (AudioClip)Resources.Load("Audio/Music/Faster Does It");
	}

	IEnumerator ChangeState(Core.State state) {
		currentState = Core.currentState;
		Debug.Log("Change to " + currentState.ToString());

		float steps = 100;
		dialogueText.text = "";
		speakerText.text = "";
		if (currentState == Core.State.Dialogue) {
			inProgress = true;
			dialogueUI.alpha = 0;
			for (int i = 0; i < steps; i++) {
				float lerp = i / steps;
				dialogueUI.alpha = lerp;
				yield return new WaitForSeconds(1 / steps);
			}
			dialogueUI.alpha = 1;
			inProgress = false;
			StartCoroutine(Next());
		}
		else {
			dialogueUI.alpha = 1;
			for (int i = 0; i < steps; i++) {
				float lerp = i / steps;
				dialogueUI.alpha = 1-lerp;
				yield return new WaitForSeconds(1 / steps);
			}
			dialogueUI.alpha = 0;
		}

		yield return null;
	}
		
	void Update () {
		if (currentState != Core.currentState) {
			StartCoroutine(ChangeState(Core.currentState));
		}

		if (Input.GetKeyDown(KeyCode.Escape)) {
			if (Core.mainMenuOpen) { StartCoroutine(Core.instance.CloseMenu()); }
			else { StartCoroutine(Core.instance.OpenMenu()); }
		}

		if (currentState == Core.State.Dialogue) {
			bool clicked = Input.GetMouseButtonDown(0);
			if (clicked && EventSystem.current.IsPointerOverGameObject()) {
				GameObject woo = EventSystem.current.currentSelectedGameObject;
				if (woo) { clicked = false; }
			}
			if (clicked || Input.GetKey(KeyCode.LeftControl)) {
				//Debug.Log("Story can continue: " + Core.story.canContinue.ToString());
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
	}
	IEnumerator Next() {
		if (inProgress) { yield break; }
		pause = false;
		inProgress = true;

		//StartCoroutine(FlashInterface(new Color(255,255,255,0.4f)));

		//PlaySound((AudioClip)Resources.Load("Audio/SFX/198449__callum-sharp279__menu-scroll-selection-sound"));

		float beat = 0;
		foreach (string tag in Core.story.currentTags) {
			Debug.Log(tag);
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
			while (!pause && Core.story.canContinue) {
				string fullText = Core.story.Continue().Trim();				
				string[] textBroken = fullText.Split('/');

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
				foreach (string tag in Core.story.currentTags) {
					Debug.Log(tag);
					string[] tagTokens = tag.Split(':');
					if (tagTokens.Length > 1) {
						if (tagTokens[0].Trim() == "atmosphere") {
							foreach (Atmosphere atmos in Core.atmospheres) {
								if (atmos.name.ToLower() == tagTokens[1].Trim().ToLower()) {
									StartCoroutine(ChangeAtmosphere(atmos));
								}
							}
						}
						if (tagTokens[0].Trim() == "bgm") {
							AudioClip snd = (AudioClip)Resources.Load("Audio/Music/" + tagTokens[1].Trim());
							if (snd != Core.bgm) { Core.bgm = snd; }
							Core.story.variablesState["bgm"] = tagTokens[1].Trim();
						}
						if (tagTokens[0].Trim() == "sfx") {
							AudioClip snd = (AudioClip)Resources.Load("Audio/SFX/Foley/" + tagTokens[1].Trim());
							Core.PlaySound(snd);
						}
					}
					else if (tag.Trim() == "gameplay") {
						Core.ChangeState(Core.State.Gameplay);
						inProgress = false;
						yield break;
					}
				}

				if (format != LineFormat.Dialogue && format != LineFormat.Parenthetical) {
					lastSpeaker = "";
					currentSpeaker = null;
				}

				if (format == LineFormat.Character) {
					lastSpeaker = fullText;
					string _name = fullText.Replace("(O.S.)", "").Replace("(COM)", "").Trim();
					for (var i = 0; i < Core.entities.Length; i++) {
						if (Core.entities[i].name.ToLower() == _name.ToLower()) { currentSpeaker = Core.entities[i]; }
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
			yield return StartCoroutine(WriteLine(linesToWrite[0].Trim(), lastLine, lastSpeaker, 0, Core.story.currentTags));
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
		pause = true;
		writing = true;
		skip = false;

		Entity entity = null;

		string[] broken = text.Split(':');
		if (broken.Length > 1) { speaker = broken[0].Trim(); text = broken[1].Trim(); }

		speaker = speaker.Replace("(O.S.)", "").Replace("(COM)", "").Trim();
		for (var i = 0; i < Core.entities.Length; i++) {
			if (Core.entities[i].name.ToLower() == speaker.ToLower()) { entity = Core.entities[i]; }
		}

		if (entity) {
			speaker = entity.nameDisplayed;
		}
		else if (speaker != "") {
			speaker = char.ToUpper(speaker[0]) + speaker.Substring(1).ToLower();
		}

		dialogueText.color = Color.white;
		if (format == LineFormat.Action) {
			Color col = Color.white;
			ColorUtility.TryParseHtmlString("#C79F73FF", out col);
			dialogueText.color = col;
		}
		speakerText.text = speaker;

		if (format == LineFormat.Dialogue) {
			//if (!skip) { Core.PlaySound((AudioClip)Resources.Load("Audio/SFX/Interface/179007__smartwentcody__book-page-turning-2"), 0.8f); }
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
		if (Core.story.currentChoices.Count > 0) {
			for (int i = 0; i < Core.story.currentChoices.Count; ++i) {
				Choice choice = Core.story.currentChoices[i];
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
		string fullText = Core.story.currentChoices[opt].text.Trim();
		Core.story.ChooseChoiceIndex(opt);
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

	IEnumerator ChangeScene(string room, bool fadeIn = true) {
		if (sceneHeading) { sceneHeading.text = room; }
		string sceneName = room.Replace("INT.", "").Replace("EXT.", "").Trim();
		Debug.Log(sceneName);
		Core.currentScene = room;
		
		string[] sceneTokens = sceneName.Split(new char[] { '-', ',' });
		foreach (string token in sceneTokens) {
			foreach (Atmosphere atmos in Core.atmospheres) {
				if (atmos.name.ToLower() == token.ToLower().Trim()) {
					StartCoroutine(ChangeAtmosphere(atmos));
					break;
				}
			}
		}

		string technicalName = sceneName.ToLower().Trim();
		if (Core.scenes.ContainsKey(technicalName)) {
			if (screenFade < 0.5f) { yield return SetFade(1, 1f); }

			Debug.Log(currentScene.name);
			if (currentScene.name != null) {
				SceneManager.UnloadSceneAsync(currentScene);
			}
			SceneManager.LoadScene(Core.scenes[technicalName],LoadSceneMode.Additive);
			currentScene = SceneManager.GetSceneByName(Core.scenes[technicalName]);

			yield return new WaitForSeconds(0.5f);
			yield return SetFade(0, 1);
		}

		AudioClip snd = null;
		foreach (string tag in Core.story.currentTags) {
			Debug.Log(tag);
			string[] tagTokens = tag.Split(':');
			if (tagTokens.Length > 0) {
				if (tagTokens[0].Trim() == "atmosphere") {
					foreach (Atmosphere atmos in Core.atmospheres) {
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

		if (snd != Core.bgm) { StartCoroutine(Core.instance.ChangeBGM(snd)); }
		Core.story.variablesState["room"] = room;

		yield break;
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

	/*
	public void Save() {
		string json = Core.CreateSaveFile().ToJson();//Core.story.state.ToJson();
		Debug.Log(json);

		StreamWriter sr = File.CreateText("Saves/save"+(Core.currentSaveSlot!=-1 ? (Core.currentSaveSlot+1).ToString() : "1")+".txt");
		sr.Write(json);
		sr.Close();

		Core.LoadSaves();
	}

	public void Load() {
		string json = "";
		if (File.Exists("Saves/save" + (Core.currentSaveSlot != -1 ? (Core.currentSaveSlot + 1).ToString() : "1") + ".txt")) {
			StreamReader sr = File.OpenText("Saves/save" + (Core.currentSaveSlot != -1 ? (Core.currentSaveSlot + 1).ToString() : "1") + ".txt");
			json = sr.ReadToEnd();
			Debug.Log(json);
			Core.story.state.LoadJson(json);
			sr.Close();
			HandleLoad();
		}
	}
	*/

	void HandleLoad() {
		Clear();
		StartCoroutine(ChangeScene(Core.story.variablesState["room"].ToString()));
		object stream = null;
		Core.story.state.jsonToken.TryGetValue("outputStream", out stream);
		if (stream != null) {
			List<object> streamObj = (List<object>)stream;
			if (streamObj.Count > 0) {
				StartCoroutine(WriteLine(streamObj[0].ToString().Substring(1), LineFormat.Dialogue));
				StartCoroutine(ChangeScene(Core.story.variablesState["room"].ToString()));
			}
			else {
				StartCoroutine(Next());
			}
		}
		StartCoroutine(Core.instance.ChangeBGM((AudioClip)Resources.Load("Audio/Music/"+Core.story.variablesState["bgm"])));
		StartCoroutine(Core.instance.CloseMenu());
	}

	void Clear() {
		for (int i = 0; i < scriptView.childCount; i++) {
			Destroy(scriptView.GetChild(i).gameObject);
		}
		Debug.Log(scriptView.childCount);
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
