using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour {

	SphereCollider triggerRange = null;
	public string onInteract = "";

	// Use this for initialization
	void Start () {
		if (!triggerRange) {
			// By default, make one with this radius. This way, every interactable object will be guaranteed to have a trigger on it.
			triggerRange = gameObject.AddComponent<SphereCollider>();
			triggerRange.radius = 3;
			triggerRange.isTrigger = true;
		}
	}

	void Awake() {
		Manager.interactables.Add(this);
	}
	void OnDestroy() {
		Manager.interactables.Remove(this);
	}

	// Update is called once per frame
	void Update () {

	}

	public void OnInteract(Transform other) {
		Debug.Log(onInteract);
		Core.StartKnot(onInteract);
	}

	/*
	void OnTriggerEnter(Collider other) {
		if (other.gameObject.tag == "Player") {
			PlayerInput _player = other.gameObject.GetComponent<PlayerInput>();
			if (_player && _player.nearbyInteractables.IndexOf(this) == -1) { _player.nearbyInteractables.Add(this); Debug.Log(this.name + " added"); }
		}
	}
	void OnTriggerExit(Collider other) {
		if (other.gameObject.tag == "Player") {
			PlayerInput _player = other.gameObject.GetComponent<PlayerInput>();
			if (_player && _player.nearbyInteractables.IndexOf(this) != -1) { _player.nearbyInteractables.Remove(this); Debug.Log(this.name + " removed"); }
		}
	}
	*/
}
