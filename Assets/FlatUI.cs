using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlatUI : MonoBehaviour {

	public RectTransform sidebar;
	public Canvas canvas;
	public RectTransform mainView;

	public GameObject initialMessagePrefab;
	private RectTransform initialMessage;
	private CanvasGroup initialMessageGroup;
	public Image scrollbarHandle;

	public Color backgroundColor = new Color();
	public Color mainColor = new Color();
	public Color accentColor = new Color();

	// Use this for initialization
	void Start () {
		//StartCoroutine(IntroSequence());
		NovelManager.instance.NewGame();
		sidebar.GetComponent<Image>().color = mainColor;
		scrollbarHandle.color = accentColor;
	}

	static FlatUI _instance;
	private static object _lock = new object();
	static public bool isActive { get { return _instance != null; } }
	static public FlatUI instance {
		get {
			lock (_lock) {
				if (_instance == null) {
					Debug.Log("[Singleton] Creating Singleton: " + FindObjectsOfType(typeof(FlatUI)).Length.ToString());

					_instance = UnityEngine.Object.FindObjectOfType(typeof(FlatUI)) as FlatUI;

					if (FindObjectsOfType(typeof(FlatUI)).Length > 1) {
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


	// Update is called once per frame
	void Update () {
		sidebar.sizeDelta = new Vector2(sidebar.sizeDelta.x,Screen.height);
		int maxPages = 5;
		if (NovelManager.instance.pages.Count > maxPages) {
			for (int i = 0; i < NovelManager.instance.pages.Count - maxPages; i++) {
				Destroy(NovelManager.instance.pages[i].gameObject);
			}
		}
	}

	IEnumerator IntroSequence() {
		initialMessage = GameObject.Instantiate(initialMessagePrefab).GetComponent<RectTransform>();
		initialMessage.SetParent(mainView);
		initialMessage.anchoredPosition3D = new Vector3();
		initialMessage.localScale = Vector3.one;
		sidebar.anchoredPosition = new Vector2(0, -Screen.height);
		yield return new WaitForSeconds(7f);
		while (sidebar.anchoredPosition.y < 0) { 
			if (sidebar.anchoredPosition.y < 0) { sidebar.anchoredPosition = new Vector2(0, sidebar.anchoredPosition.y + Time.fixedDeltaTime * 400); }
			if (sidebar.anchoredPosition.y > 0) { sidebar.anchoredPosition = new Vector2(0, 0); }
			yield return new WaitForSeconds(Time.fixedDeltaTime);
		}
		while (initialMessage.anchoredPosition.y < Screen.height*0.5f+100) {
			if (initialMessage.anchoredPosition.y < Screen.height * 0.5f + 100) { initialMessage.anchoredPosition = new Vector2(0, initialMessage.anchoredPosition.y + Time.fixedDeltaTime * 400); }
			yield return new WaitForSeconds(Time.fixedDeltaTime);
		}
		Destroy(initialMessage.gameObject);
		yield return null;
	}
}
