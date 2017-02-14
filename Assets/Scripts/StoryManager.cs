﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using Ink.Runtime;

public class StoryManager : MonoBehaviour {

	public TextAsset _mainStory;
	public static TextAsset mainStory = null;
	public static Story story;

	public static SaveFile lastSave = null;

	public AudioLowPassFilter lowPassFilter = null;
	public AudioHighPassFilter highPassFilter = null;

	public enum LineFormat {
		Action,
		Character,
		Dialogue,
		Parenthetical,
		Transition,
		SceneHeading,
		MissionHeading
	}

	public static bool inProgress = false;

	public static SceneRedirect[] scenes = new SceneRedirect[0];
	public static Entity[] entities = new Entity[0];
	public static Atmosphere[] atmospheres = new Atmosphere[0];
	public static Dictionary<string, World> worlds = new Dictionary<string, World>();
	
	static StoryManager _instance;
	private static object _lock = new object();
	static public bool isActive { get { return _instance != null; } }
	static public StoryManager instance {
		get {
			lock (_lock) {
				if (_instance == null) {
					Debug.Log("[Singleton] Creating Singleton: " + FindObjectsOfType(typeof(StoryManager)).Length.ToString());

					_instance = UnityEngine.Object.FindObjectOfType(typeof(StoryManager)) as StoryManager;

					if (FindObjectsOfType(typeof(StoryManager)).Length > 1) {
						Debug.LogError("[Singleton] Something went really wrong " +
							" - there should never be more than 1 singleton!" +
							" Reopening the scene might fix it.");
						DontDestroyOnLoad(_instance.gameObject);
						return _instance;
					}

					if (_instance == null) {
						GameObject go = new GameObject("StoryManager Story");
						DontDestroyOnLoad(go);
						_instance = go.AddComponent<StoryManager>();
						Debug.Log("[Singleton] An instance of " + typeof(StoryManager) +
							" is needed in the scene, so '" + go.name +
							"' was created with DontDestroyOnLoad.");
					}
					else {
						DontDestroyOnLoad(_instance.gameObject);
						Debug.Log("[Singleton] Using instance already created: " +
							_instance.gameObject.name);
					}
				}
				return _instance;
			}
		}
	}

	public static string[] saveSlots = new string[3];
	public static int mostRecentSave = -1;
	public static int currentSaveSlot = -1;

	public static string currentScene = "";

	private static AudioSource _bgm;

	public static AudioClip bgm {
		get {
			if (!_bgm) { return null; }
			return _bgm.clip;
		}
		set {
			if (!_bgm) { return; }
			_bgm.clip = value;
			_bgm.Stop();
			if (_bgm.clip) { _bgm.Play(); }
		}
	}

	public static string GetScene(string _name) {
		foreach (SceneRedirect redirect in scenes) {
			foreach (string n in redirect.aliases) {
				if (n.ToLower().Trim() == _name.ToLower().Trim()) {
					return redirect.sceneName;
				}
			}
		}
		return "";
	}	

	// Use this for initialization
	void OnEnable() {
		Debug.Log(StoryManager.instance.name);

		mainStory = _mainStory;

		/*
		Transform __bgm = transform.FindChild("BGM");
		if (!__bgm) {
			__bgm = new GameObject("BGM").transform;
			__bgm.SetParent(transform);
		}
		_bgm = __bgm.GetComponent<AudioSource>();
		if (!_bgm) { _bgm = __bgm.gameObject.AddComponent<AudioSource>(); }
		_bgm.outputAudioMixerGroup = Manager.instance.mainMixer.FindMatchingGroups("Master")[0];
		_bgm.loop = true;
		*/

		/*
		if (!_bgm.GetComponent<AudioLowPassFilter>()) { _bgm.gameObject.AddComponent<AudioLowPassFilter>(); }
		if (!_bgm.GetComponent<AudioHighPassFilter>()) { _bgm.gameObject.AddComponent<AudioHighPassFilter>(); }
		lowPassFilter = _bgm.GetComponent<AudioLowPassFilter>();
		highPassFilter = _bgm.GetComponent<AudioHighPassFilter>();
		lowPassFilter.cutoffFrequency = 1750;
		lowPassFilter.lowpassResonanceQ = 2;
		highPassFilter.cutoffFrequency = 1000;
		highPassFilter.highpassResonanceQ = 2;
		*/

		StoryManager.LoadSaves();

		entities = Resources.LoadAll<Entity>("Entities");
		Debug.Log(entities.Length + " entities loaded");

		atmospheres = Resources.LoadAll<Atmosphere>("Atmospheres");
		Debug.Log(atmospheres.Length + " atmospheres loaded");

		scenes = Resources.LoadAll<SceneRedirect>("Databases/Scene Redirects");
		Debug.Log(scenes.Length + " scene redirects loaded");
		/*
		scenes["nox, high bridge"] = "nox_highbridge";
		scenes["courtroom"] = "courtroom";
		scenes["virrea"] = "virrea";
		*/

		if (story == null) {
			story = new Story(mainStory.text);
			story.BindExternalFunction("getReputation", (string name) => {
				Entity entity = GetEntity(name);
				return GetReputation(entity);
			});
		}
	}

	public static void StartKnot(string knot) {
		StoryManager.story.ChoosePathString(knot);
		Manager.instance.StartCoroutine(Manager.instance.ChangeState(Manager.State.Dialogue));
		//Debug.Log(StoryManager.story.currentText);
		//StoryManager.story.Continue();
	}

	public static LineFormat GetFormat(string fullText, LineFormat lastLine = LineFormat.Action) {
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
		else if ((fullText[0] == '@' || fullText.ToUpper() == fullText) && fullText != "...") { format = LineFormat.Character; }
		else if ((lastLine == LineFormat.Character || lastLine == LineFormat.Dialogue) && fullText.StartsWith("(") && fullText.EndsWith(")")) { format = LineFormat.Parenthetical; }
		else if (dialogueBroken.Length > 1 || lastLine == LineFormat.Character || lastLine == LineFormat.Parenthetical) { format = LineFormat.Dialogue; }

		return format;
	}


	public IEnumerator ChangeBGM(AudioClip clip = null) {
		float steps = 100;
		if (_bgm.isPlaying) {
			for (int i = 0; i < steps; i++) {
				float lerp = i / steps;
				_bgm.volume = 1 - lerp;
				yield return new WaitForSeconds(0.01f);
			}
		}
		_bgm.volume = 0;
		StoryManager.bgm = clip;
		if (clip != null) {
			for (int i = 0; i < steps; i++) {
				float lerp = i / steps;
				_bgm.volume = lerp;
				yield return new WaitForSeconds(0.01f);
			}
		}
		_bgm.volume = 1;

		yield return null;
	}

	public static AudioSource PlaySound(AudioClip clip, float volume = 1, float pitch = 1) {
		GameObject obj = new GameObject();
		AudioSource src = obj.AddComponent<AudioSource>();
		src.clip = clip;
		src.volume = volume;
		src.pitch = pitch;
		src.outputAudioMixerGroup = Manager.instance.mainMixer.FindMatchingGroups("Master")[0];
		src.Play();
		Destroy(obj, clip.length);
		return src;
	}

	public static SaveFile CreateSaveFile() {
		SaveFile save = new SaveFile();
		save.storyState = story.state.ToJson();
		save.lastSaved = System.DateTime.Now.ToString();
		save.room = currentScene;
		// Include position of player, and actual Scene
		save.bgm = bgm ? bgm.name : "";
		save.posX = Manager.player.transform.position.x;
		save.posY = Manager.player.transform.position.y;
		save.posZ = Manager.player.transform.position.z;
		save.currentState = Manager.currentState.ToString();
		return save;
	}

	public static void LoadSaveFile(int index) {
		currentSaveSlot = index;
		SaveFile save = JsonUtility.FromJson<SaveFile>(StoryManager.saveSlots[index]);
		StoryManager.LoadSaveFile(save);
	}
	public static void LoadSaveFile(SaveFile save) {
		lastSave = save;
		story.state.LoadJson(save.storyState);
		Debug.Log(save.storyState);

		try {
			Manager.State state = (Manager.State)System.Enum.Parse(typeof(Manager.State), save.currentState);
			Manager.instance.ChangeState(state);
		}
		catch {
			Manager.instance.ChangeState(Manager.State.Dialogue);
		}

		Debug.Log("Loaded");
		// Reset the scene (and the Manager with it).
		SceneManager.LoadScene("main");
	}
	
	public static void LoadSaves() {
		System.DateTime mostRecent = new System.DateTime();
		for (int i = 1; i <= 3; i++) {
			string json = "";
			string filename = "Saves/save" + i.ToString() + ".txt";
			if (File.Exists(filename)) {
				StreamReader sr = File.OpenText(filename);

				json = sr.ReadToEnd();
				SaveFile save = JsonUtility.FromJson<SaveFile>(json);

				System.DateTime dateTime = new System.DateTime();
				if (save != null && save.lastSaved != "") { System.DateTime.TryParse(save.lastSaved, out dateTime); }
				//Debug.Log(dateTime.ToString());
				if (StoryManager.mostRecentSave == -1 || dateTime.CompareTo(mostRecent) >= 0) { StoryManager.mostRecentSave = i - 1; mostRecent = dateTime; }
				//Debug.Log(json);
				//saveSlots[i] = new Story(StoryManager.mainStory.text); //saveSlots[i].state.LoadJson(json);
				saveSlots[i - 1] = json;
				//Debug.Log(saveSlots[i].variablesState["playthrough"]);
				sr.Close();
			}
		}
	}

	void Update() {
		//lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency, mainMenuOpen ? 1750 : 9999, Time.fixedDeltaTime);
		//highPassFilter.cutoffFrequency = Mathf.Lerp(highPassFilter.cutoffFrequency, mainMenuOpen ? 1000 : 0, Time.fixedDeltaTime);
	}

	float GetReputation(Entity entity) {
		float affection = 0;
		float respect = 0;
		float trust = 0;
		if (entity) {
			foreach (string s in entity.likes) { affection += (float)story.variablesState[s]; }
			foreach (string s in entity.dislikes) { affection -= (float)story.variablesState[s]; }
			foreach (string s in entity.respects) { respect += (float)story.variablesState[s]; }
			foreach (string s in entity.disrespects) { respect -= (float)story.variablesState[s]; }
			foreach (string s in entity.trusts) { trust += (float)story.variablesState[s]; }
			foreach (string s in entity.distrusts) { trust -= (float)story.variablesState[s]; }
		}
		float result = (affection + respect + trust) / 3;
		Debug.Log(result);
		return result;
	}

	Entity GetEntity(string name) {
		for (int i = 0; i < entities.Length; i++) {
			if (entities[i].name == name) { return entities[i]; }
		}
		return null;
	}
}
