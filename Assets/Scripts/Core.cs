using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using Ink.Runtime;

public class Core : MonoBehaviour {

	public TextAsset _mainStory;
	public static TextAsset mainStory = null;
	public static Story story;

	public AudioLowPassFilter lowPassFilter = null;
	public AudioHighPassFilter highPassFilter = null;

	public static CameraControl mainCamera = null;

	public static State currentState = State.Dialogue;
	public enum State {
		Dialogue,
		Menu,
		Gameplay
	}

	public static Canvas mainCanvas = null;
	public static GameObject mainMenu = null;
	public static bool mainMenuOpen = false;

	public static bool inProgress = false;

	public static Dictionary<string, string> scenes = new Dictionary<string, string>();

	public static Entity[] entities = new Entity[0];
	public static Atmosphere[] atmospheres = new Atmosphere[0];
	public static Dictionary<string, World> worlds = new Dictionary<string, World>();
	
	static Core _instance;
	private static object _lock = new object();
	static public bool isActive { get { return _instance != null; } }
	static public Core instance {
		get {
			lock (_lock) {
				if (_instance == null) {
					Debug.Log("[Singleton] Creating Singleton: " + FindObjectsOfType(typeof(Core)).Length.ToString());

					_instance = UnityEngine.Object.FindObjectOfType(typeof(Core)) as Core;

					if (FindObjectsOfType(typeof(Manager)).Length > 1) {
						Debug.LogError("[Singleton] Something went really wrong " +
							" - there should never be more than 1 singleton!" +
							" Reopening the scene might fix it.");
						DontDestroyOnLoad(_instance.gameObject);
						return _instance;
					}

					if (_instance == null) {
						GameObject go = new GameObject("Core Story");
						DontDestroyOnLoad(go);
						_instance = go.AddComponent<Core>();
						Debug.Log("[Singleton] An instance of " + typeof(Core) +
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

	public static void ChangeState(State _state) {
		currentState = _state;
	}

	public static void StartKnot(string knot) {
		ChangeState(State.Dialogue);
		Core.story.ChoosePathString(knot);
		//Debug.Log(Core.story.currentText);
		//Core.story.Continue();
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
		Core.bgm = clip;
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
		src.Play();
		Destroy(obj, clip.length);
		return src;
	}

	public IEnumerator OpenMenu() {
		if (mainMenuOpen) {yield break; }
		mainMenuOpen = true;
		mainMenu.SetActive(true);

		CanvasGroup group = mainMenu.GetComponent<CanvasGroup>();
		if (group) {
			group.alpha = 0;
			float steps = 100;
			float fadeTime = 0.2f;
			for (int i = 0; i < steps; i++) {
				float lerp = (i/steps);
				group.alpha = lerp;
				yield return new WaitForSeconds(fadeTime / steps);
			}
			group.alpha = 1;
		}

		yield return null;
	}

	public IEnumerator CloseMenu() {
		if (!mainMenuOpen) { yield break; }
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

	void OnSceneWasChanged(Scene previousScene, Scene nextScene) {
		mainCanvas = GameObject.FindObjectOfType<Canvas>();
		Debug.Log(mainCanvas);

		mainMenu = GameObject.Find("Main Menu");
		if (!mainMenu) {
			mainMenu = GameObject.Instantiate((GameObject)Resources.Load("Prefabs/Utility/Main Menu"));
		}
		mainMenu.transform.SetParent(mainCanvas.transform);
		RectTransform _rect = mainMenu.GetComponent<RectTransform>();
		_rect.sizeDelta = new Vector2(0, 0);
		_rect.anchoredPosition = new Vector2(0, 0);
		mainMenu.SetActive(mainMenuOpen);
	}
	void OnSceneWasLoaded(Scene _scene, LoadSceneMode mode) {
		Camera[] cameras = GameObject.FindObjectsOfType<Camera>();
		if (cameras.Length > 1) {
			int camerasDestroyed = 0;
			for (int i = 0; i < cameras.Length; i++) {
				CameraControl cc = cameras[i].GetComponent<CameraControl>();
				if (!cc && cameras[i] != mainCamera && !(i == cameras.Length-1 && camerasDestroyed == cameras.Length-1)) {
					camerasDestroyed++;
					Destroy(cameras[i].gameObject);
				}
			}
		}

		GameObject _player = GameObject.FindGameObjectWithTag("Player");
		if (!_player) {	_player = GameObject.Instantiate((GameObject)Resources.Load("Prefabs/Player")); }

		PositionMarker[] markers = GameObject.FindObjectsOfType<PositionMarker>();
		_player.transform.position = new Vector3();
		for (int i = 0; i < markers.Length; i++) {
			if (markers[i].defaultEntry) { _player.transform.position = markers[i].transform.position; }
		}
		_player.GetComponent<Character>().movement = _player.transform.position;

		mainCamera.relativeTo = _player.transform;
	
		foreach (GameObject g in GameObject.FindGameObjectsWithTag("Invisible")) {
			MeshRenderer mr = g.GetComponent<MeshRenderer>();
			if (mr) { mr.enabled = false; }
		}
	}
	void OnSceneWasUnloaded(Scene _scene) {
	}

	public static SaveFile CreateSaveFile() {
		SaveFile save = new SaveFile();
		save.storyState = story.state.ToJson();
		save.lastSaved = System.DateTime.Now.ToString();
		save.room = currentScene;
		// Include position of player, and actual Scene
		save.bgm = bgm ? bgm.name : "";
		return save;
	}

	public static void LoadSaveFile(SaveFile save) {
		story.state.LoadJson(save.storyState);
	}

	// Use this for initialization
	void OnEnable () {
		Debug.Log(Core.instance.name);

		SceneManager.activeSceneChanged += OnSceneWasChanged;
		SceneManager.sceneLoaded += OnSceneWasLoaded;
		SceneManager.sceneUnloaded += OnSceneWasUnloaded;
		mainStory = _mainStory;

		CameraControl cc = Camera.main.GetComponent<CameraControl>();
		if (cc) {
			mainCamera = cc;
		}
		else {
			mainCamera = Camera.main.gameObject.AddComponent<CameraControl>();
		}

		Transform __bgm = transform.FindChild("BGM");
		if (!__bgm) {
			__bgm = new GameObject("BGM").transform;
			__bgm.SetParent(transform);			
		}
		_bgm = __bgm.GetComponent<AudioSource>();
		if (!_bgm) { _bgm = __bgm.gameObject.AddComponent<AudioSource>(); }
		_bgm.loop = true;

		if (!_bgm.GetComponent<AudioLowPassFilter>()) { _bgm.gameObject.AddComponent<AudioLowPassFilter>(); }
		if (!_bgm.GetComponent<AudioHighPassFilter>()) { _bgm.gameObject.AddComponent<AudioHighPassFilter>(); }
		lowPassFilter = _bgm.GetComponent<AudioLowPassFilter>();
		highPassFilter = _bgm.GetComponent<AudioHighPassFilter>();
		lowPassFilter.cutoffFrequency = 1750;
		lowPassFilter.lowpassResonanceQ = 2;
		highPassFilter.cutoffFrequency = 1000;
		highPassFilter.highpassResonanceQ = 2;

		Core.LoadSaves();

		entities = Resources.LoadAll<Entity>("Entities");
		Debug.Log(entities.Length + " entities loaded");

		atmospheres = Resources.LoadAll<Atmosphere>("Atmospheres");
		Debug.Log(atmospheres.Length + " atmospheres loaded");

		scenes["nox, high bridge"] = "nox_highbridge";
		scenes["courtroom"] = "courtroom";
		scenes["virrea"] = "virrea";

		if (story == null) {
			story = new Story(mainStory.text);
			story.BindExternalFunction("getReputation", (string name) => {
				Entity entity = GetEntity(name);
				return GetReputation(entity);
			});
		}
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
				if (Core.mostRecentSave == -1 || dateTime.CompareTo(mostRecent) >= 0) { Core.mostRecentSave = i - 1; mostRecent = dateTime; }
				//Debug.Log(json);
				//saveSlots[i] = new Story(Core.mainStory.text); //saveSlots[i].state.LoadJson(json);
				saveSlots[i - 1] = json;
				//Debug.Log(saveSlots[i].variablesState["playthrough"]);
				sr.Close();
			}
		}
	}

	void Update() {
		lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency, mainMenuOpen ? 1750 : 9999, Time.fixedDeltaTime);
		highPassFilter.cutoffFrequency = Mathf.Lerp(highPassFilter.cutoffFrequency, mainMenuOpen ? 1000 : 0, Time.fixedDeltaTime);
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
