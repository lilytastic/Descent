using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerInput : MonoBehaviour {

	public Character character;

	public Vector3 input = new Vector3();
	public bool dashPressed = false;
	public bool firePressed = false;
	public float mouseWheel = 0;

	public Interactable nearbyInteractable = null;

	public Collider interactRange;

	public List<Interactable> nearbyInteractables = new List<Interactable>();
	public int currentlySelectedInteractable = 0;

	// Use this for initialization
	void Start() {
		character = transform.GetComponent<Character>();
	}

	// Update is called once per frame
	void Update() {
		input = new Vector3();

		// If no input allowed, we reset all inputs and return before adjusting them at all.
		if (Core.currentState != Core.State.Gameplay) { return; }

		Vector3 euler = Camera.main.transform.eulerAngles;
		Camera.main.transform.eulerAngles = new Vector3(90, euler.y, 0);
		input += Camera.main.transform.right * Input.GetAxis("Horizontal");
		input += Camera.main.transform.up * Input.GetAxis("Vertical");
		input = input.normalized;
		input = Vector3.ProjectOnPlane(input, Vector3.up);

		Camera.main.transform.eulerAngles = euler;

		if (Input.GetAxis("Dash") <= 0.5f && dashPressed) { dashPressed = false; }
		if (Input.GetAxis("Fire1") <= 0.5f && firePressed) { firePressed = false; }

		/*
		nearbyInteractable = null;
		float _dist = -1;
		foreach (Interactable _int in Manager.interactables) {
			float dist = Vector3.Distance(_int.transform.position, transform.position);
			if (dist > 2) { continue; }
			if (_dist == -1 || dist < _dist) {
				nearbyInteractable = _int;
				_dist = dist;
			}
		}

		if (Input.GetKeyDown(KeyCode.R)) {
			if (character.currentGadgets[0] != null) {
				Debug.Log(character.currentGadgets[0].name);
				EffectorComponent[] eff = character.currentGadgets[0].GetEffectors();
				foreach (EffectorComponent ef in eff) {
					if (ef is FiringMechanism) {
						FiringMechanism mechanism = ef as FiringMechanism;
						StartCoroutine(character.Reload(mechanism));
					}
				}
			}
		}
		*/
		nearbyInteractable = null;
		float _dist = -1;
		foreach (Interactable _int in Manager.interactables) {
			Vector3 p2 = _int.transform.position; p2 = new Vector3(p2.x, 0, p2.y);
			Vector3 p1 = transform.position; p1 = new Vector3(p1.x, 0, p1.y);
			float dist = Vector3.Distance(p2, p1);
			if (dist > 2) { continue; }
			if (_dist == -1 || dist < _dist) {
				nearbyInteractable = _int;
				_dist = dist;
			}
		}

		if (Input.GetKeyDown(KeyCode.E)) {
			if (nearbyInteractable != null) {
				//nearbyInteractables[currentlySelectedInteractable].SendMessage("OnInteract", transform);
				nearbyInteractable.OnInteract(transform);
			}
		}

		if (character.movementType == Character.MovementType.Running) {
			character.movement = transform.position + input.normalized;
			/*
			if (!character.isStunned) {
				character.direction = (Manager.cursor - transform.position).normalized;
			}

			mouseWheel += Input.GetAxis("Mouse ScrollWheel") * 10;

			if (!character.isStunned) {
				Gadget primary = character.currentGadgets[0];
				if (primary != null && Input.GetAxis("Fire1") > 0.5f && (primary.isAutomatic || !firePressed)) {
					firePressed = true;
					Vector3 targ = ModulateAttackTarget(Manager.cursor);
					character.UseGadget(0, targ); //character.attacks.Add();
				}
			}
			*/
		}
		if (Input.GetAxis("Dash") > 0.5f && !dashPressed) {
			dashPressed = true;
			if (input.magnitude == 0) { input = character.direction; }
			character.Dash(input);
		}
	}

	/*
	public Vector3 ModulateAttackTarget(Vector3 _attackTarget) {
		float accuracy = 90;
		float rand = ((100 - accuracy) / 100) * 5;
		_attackTarget += new Vector3(Random.Range(-rand, rand), 0, Random.Range(-rand, rand));
		return _attackTarget;
	}
	*/

}
