using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class GameManager : MonoBehaviour {

	static GameManager _instance;
	private static object _lock = new object();
	static public bool isActive { get { return _instance != null; } }
	static public GameManager instance {
		get {
			lock (_lock) {
				if (_instance == null) {
					Debug.Log("[Singleton] Creating Singleton: " + FindObjectsOfType(typeof(GameManager)).Length.ToString());

					_instance = UnityEngine.Object.FindObjectOfType(typeof(GameManager)) as GameManager;

					if (FindObjectsOfType(typeof(UIManager)).Length > 1) {
						Debug.LogError("[Singleton] Something went really wrong " +
							" - there should never be more than 1 singleton!" +
							" Reopening the scene might fix it.");
						return _instance;
					}

					if (_instance != null) {
						Debug.Log("[Singleton] Using instance already created: " +
							_instance.gameObject.name);
					}
				}
				return _instance;
			}
		}
	}

	public ScreenManager mainScreenManager;
	public ScreenManager mainMenuScreenManager;
	public ScreenManager pauseScreenManager;

	public TransitionScreen transition;

	public string currentScene = "";

	public bool stopWriting = false;

	public AudioMixer mainMixer;

	public AudioClip soundDiceRoll;

	public int latestDiceRoll = -1;
	public int latestDiceRollBeforeModifiers = -1;
	public int latestDiceRollRequirement = -1;
	public int Roll(float req, float mod) {
		Debug.Log("Test");
		int _roll = Random.Range(1,20) + Mathf.RoundToInt(mod);
		latestDiceRoll = _roll;
		latestDiceRollBeforeModifiers = _roll - Mathf.RoundToInt(mod);
		latestDiceRollRequirement = Mathf.RoundToInt(req);

		if (latestDiceRollBeforeModifiers == 1) {return 0;} // Critical miss
		if (_roll >= req || latestDiceRollBeforeModifiers == 20) {return 1;} // Success
		return 0;
	}

	public enum GameState {
		MainMenu,
		PauseMenu,
		Story,
		Game
	}

	public enum ScreenState {
		Main,
		Game,
		Story,
		Adventure,
		Pause,
		Load,
		Save,
		Options,
		Transition
	}

	public GameState state = GameState.MainMenu;

	public HealthBar healthBar;

	public ScreenManager gameScreen;

	//public GameScreen[] screens = new GameScreen[0];

	// Use this for initialization
	void Start () {
		//NewGame();
		//screens = GameObject.FindObjectsOfType<GameScreen>();
		if (mainScreenManager == null) {mainScreenManager = transform.GetOrAddComponent<ScreenManager>();}
	}

	public void NewGame() {
		NovelManager.instance.ClearText();
		NovelManager.instance.ClearChoices();
		state = GameState.Story;
		mainScreenManager.state = ScreenState.Game;

		StartCoroutine(NovelManager.instance.Next());
	}
	
	public void ChangeScene(string _scene) {
		StartCoroutine(_ChangeScene(_scene));
	}
	IEnumerator _ChangeScene(string _scene) {
		stopWriting = true;
		ScreenState _prev = gameScreen.state;
		transition.nameOfScene.text = _scene;
		gameScreen.state = ScreenState.Transition;
		yield return new WaitForSeconds(3f);
		gameScreen.state = _prev;
		yield return new WaitForSeconds(2f);
		stopWriting = false;
		//StartCoroutine(NovelManager.instance.Next());
		yield return null;
	}

	public void ChangeMaxHealth(float current) {
		healthBar.healthMax = current;
	}

	public void ChangeHealth(float current) {
		StartCoroutine(_ChangeHealth(current));
	}
	IEnumerator _ChangeHealth(float current) {
		stopWriting = true;
		healthBar.health = current;
		yield return new WaitForSeconds(1f);
		stopWriting = false;
		yield return null;
	}

	public AudioSource PlaySound(AudioClip clip, float volume = 1, float pitch = 1) {
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

	private ScreenState stateBeforePausing = ScreenState.Main;
	void Update () {
		if (Input.GetKeyDown(KeyCode.Escape)) {
			if (mainScreenManager.state == ScreenState.Main) {
				if (mainMenuScreenManager.state != ScreenState.Main) {mainMenuScreenManager.state = ScreenState.Main;}
			}
			else if (mainScreenManager.state == ScreenState.Game) {
				if (gameScreen.state != ScreenState.Pause) {
					stateBeforePausing = gameScreen.state; gameScreen.state = ScreenState.Pause;
				}
				else {
					if (pauseScreenManager.state == ScreenState.Main) {
						gameScreen.state = stateBeforePausing;
					}
					else {
						pauseScreenManager.state = ScreenState.Main;
					}
				}
			}
		}
	}
}
