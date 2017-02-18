using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
	public ScreenManager pauseScreenManager;

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
		Options
	}

	public GameState state = GameState.MainMenu;

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
	

	private ScreenState stateBeforePausing = ScreenState.Main;
	void Update () {
		if (Input.GetKeyDown(KeyCode.Escape)) {
			if (mainScreenManager.state == ScreenState.Game) {
				if (gameScreen.state != ScreenState.Pause) {stateBeforePausing = gameScreen.state; gameScreen.state = ScreenState.Pause;}
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
