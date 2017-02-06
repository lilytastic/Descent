using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Character : MonoBehaviour {
	[System.Serializable]
	public class Stats {
		public float maxHealth = 100;
		public float maxFocus = 100;

		public float vigor = 50; // Strength/Constitution (Health, equip load, attack damage)
		public float grace = 50; // Dexterity/Agility/Charisma (Speed, accuracy, dodge speed)
		public float intellect = 50; // Intelligence/Willpower (Focus, item identification, crafting and energy use)

		public int level = 1;
		public float experiencePoints = 0;
		public float experienceGranted = 100;
		
		public int skillPoints = 0;

		// Determines roll speed.
		public float weight = 1;
		// Amount to reduce all damage by.
		public float damageReduction = 0;

		public Stats() { }
	}

	public Stats stats = new Stats();

	public bool isAlive = true;

	public string[] factions = new string[0];

	public enum DeathType {
		BecomeCorpse,
		Explode,
		Collapse,
		None,
		GameOver
	};
	public DeathType onDeath = DeathType.BecomeCorpse;

	[HideInInspector]
	public Vector3 velocity = new Vector3();

	[HideInInspector]
	public int recentHits = 0;
	[HideInInspector]
	public float timeSinceLastHit = 0;

	Rigidbody _rigidbody;
	public CharacterController _characterController;
	public SphereCollider _colliderTrigger;
	public Transform armature;
	SpriteRenderer characterGraphic;
	SpriteRenderer weaponGraphic;
	public Animator animator;

	Transform chestBone;
	Transform neckBone;
	Transform weaponLeft;
	Transform weaponRight;

	[HideInInspector]
	public List<Coroutine> attacks = new List<Coroutine>();

	[HideInInspector]
	public Vector3 movement = new Vector3();
	public enum MovementType {
		Running,
		Dashing
	}
	[HideInInspector]
	public MovementType movementType = MovementType.Running;

	[HideInInspector]
	public float bodyRotation = 0;
	[HideInInspector]
	public float fireRotation = 0;

	[HideInInspector]
	public Vector3 direction = new Vector3();
	[HideInInspector]
	public Vector3 fireDirection = new Vector3();

	Quaternion neckRotation = new Quaternion();
	Quaternion chestRotation = new Quaternion();

	[HideInInspector]
	public float[] equipTimer = new float[2];

	private bool stunnedWhileAttacking = false;
	public float stunTimer = 0;
	public bool isStunned { get { return (stunnedWhileAttacking || stunTimer > 0); } }

	public float health = 100;

	public float soundVolume = 0.5f;

	// For drawing reload progress.
	bool isReloading = false;
	public float[] reloadTime = new float[2];
	public float[] reloadProgress = new float[2];

	public float drawSpeed = 1;
	
	public float invincibilityFrames = 0;
	public bool useInvincibilityFrames = false;

	public float equipLoad = 0;

	public float speed = 5;
	public float acceleration = 90;
	public float deceleration = 60;

	public float dashSpeed = 40;
	private float _dashSpeed { get { return dashSpeed * (stats.weight - equipLoad / stats.vigor); } }
	public float dashDeceleration = 6;
	public float dashEndVelocity = 2f;
	public float dashSweetSpot = 0.6f;
	public float dashCooldown = 0.2f;
	public float dashLength = 1f;
	public GameObject dashEffect;
	public enum DashType {
		Straight,
		Linear,
		Curved,
		Charged
	}
	public DashType dashType = DashType.Straight;
	[HideInInspector]
	public float dashTimeSpent = 0;
	[HideInInspector]
	public float dashCooldownSpent = 0;

	public int dashChain = 0;

	public AudioClip dashSound;
	public AudioClip dashEndSound;
	public AudioClip dashChainSound;

	public int bulletsInSequence = 0;
	

	public GameObject hitSpark;

	// Use this for initialization
	void Start() {
		movement = transform.position;
		//characterGraphic = transform.FindChild("Character").GetComponent<SpriteRenderer>();
		animator = transform.GetComponentInChildren<Animator>();
		_rigidbody = transform.GetComponent<Rigidbody>();
		_characterController = transform.GetComponent<CharacterController>();
		_colliderTrigger = transform.GetComponent<SphereCollider>();

		Transform[] transforms = gameObject.GetComponentsInChildren<Transform>();
		if (!chestBone) { foreach (Transform t in transforms) { if (t.name == "Chest") { chestBone = t; break; } } }
		if (!neckBone) { foreach (Transform t in transforms) { if (t.name == "Neck") { neckBone = t; break; } } }
		if (!weaponLeft) { foreach (Transform t in transforms) { if (t.name == "Gadget_R") { weaponLeft = t; break; } } }
		if (!weaponRight) { foreach (Transform t in transforms) { if (t.name == "Gadget_L") { weaponRight = t; break; } } }		
	}
	void Awake() {
		//Manager.characters.Add(this);
		
		if (armature) {
			direction = armature.forward;
		}
		//chestRotation = transform.rotation;
		//neckRotation = transform.rotation;
	}
	void OnDestroy() {
		//Manager.characters.Remove(this);
	}

	// Update is called once per frame
	void Update() {
		_characterController.enabled = isAlive;
		_colliderTrigger.enabled = isAlive;
		if (isAlive) {
			UpdateData();
			UpdateMovement();
			UpdateAnimation();
			UpdateGraphics();
		}
	}

	void LateUpdate() {
		if (isAlive) {
			fireDirection.y = 0;
			if (movementType == MovementType.Running && !isStunned) {
				if (chestBone) {
					if (fireDirection != Vector3.zero) {
						chestRotation = Quaternion.Slerp(chestRotation, Quaternion.LookRotation(fireDirection, Vector3.up) * Quaternion.Euler(0, 0, -90), Time.deltaTime * 6);
					}
					chestBone.rotation = chestRotation;
				}
				if (neckBone) {
					//neckBone.rotation = Quaternion.LookRotation(fireDirection, Vector3.up) * Quaternion.Euler(0, 0, -90);
					if (fireDirection != Vector3.zero) {
						neckRotation = Quaternion.Slerp(neckRotation, Quaternion.LookRotation(fireDirection, Vector3.up) * Quaternion.Euler(0, 0, -90), Time.deltaTime * 30); ;
					}
					neckBone.rotation = neckRotation;
				}
			}
		}
		//transform.position = new Vector3(transform.position.x, Manager.baseY, transform.position.z);
	}

	public void UpdateData() {
		timeSinceLastHit += Time.deltaTime;
		if (timeSinceLastHit > 1.1f) { recentHits = 0; }
		if (timeSinceLastHit > 4) {
			// Invincibility frames stop
		}

		if (dashCooldownSpent > 0) {
			float prev = dashCooldownSpent;
			dashCooldownSpent += Time.deltaTime;
			if (prev <= dashSweetSpot && dashCooldownSpent > dashSweetSpot) {
				//Manager.PlaySound(dashEndSound, transform.position);
			}
			if (dashCooldownSpent >= dashCooldown) {
				dashCooldownSpent = 0;
				dashChain = 0;
			}
		}

		if (stunTimer > 0) { stunTimer -= Time.deltaTime; if (stunTimer < 0) { stunTimer = 0; } }
		if (invincibilityFrames > 0) { invincibilityFrames -= Time.deltaTime; if (invincibilityFrames < 0) { invincibilityFrames = 0; } }
	}

	public void UpdateMovement() {
		Vector3 _movement = (movement - transform.position).normalized;

		if (movementType == Character.MovementType.Running) {
			if (isStunned || _movement.magnitude < 0.2f) { Decelerate(deceleration); } // || (currentGadget != null && currentGadget.firing)
			else { velocity = Vector3.ClampMagnitude(velocity + (_movement * speed - velocity) * Mathf.Clamp01(Time.deltaTime * acceleration), speed); }
		}
		else if (movementType == Character.MovementType.Dashing) {
			//Decelerate(dashDeceleration);
			velocity = velocity.normalized * (_dashSpeed + 0.05f * Mathf.Clamp(dashChain, 0, 4));
			dashTimeSpent += Time.deltaTime;
			if (dashTimeSpent >= dashLength) {
				EndDash();
			}
		}

		Vector3 relativeVelocity = velocity; // Vector3.ProjectOnPlane(velocity,Vector3.up);
		if (_rigidbody) { _rigidbody.velocity = relativeVelocity; }
		//else if (_navMeshAgent) { _navMeshAgent.SetDestination(movement); }
		else if (_characterController) { _characterController.SimpleMove(relativeVelocity); }
		else { transform.Translate(relativeVelocity * Time.deltaTime); }

		fireDirection = direction;
		//fireDirection.y = 0;
	}

	public void UpdateAnimation() {
		if (animator) {
			animator.SetBool("Running", Mathf.Abs(velocity.magnitude) > 0.1);
			animator.SetFloat("Speed", velocity.magnitude);
			animator.SetFloat("Relative Direction", Mathf.DeltaAngle(fireRotation, bodyRotation));

			if (!isStunned) {
				if (velocity.magnitude > 0.2f) { }

				float targetBodyRotation = Mathf.Atan2(-velocity.z, velocity.x) * Mathf.Rad2Deg;
				fireRotation = targetBodyRotation;
				if (movementType == MovementType.Running) {
					fireRotation = Mathf.Atan2(-direction.z, direction.x) * Mathf.Rad2Deg;
					if (Mathf.Abs(Mathf.DeltaAngle(fireRotation, targetBodyRotation)) > 90) { targetBodyRotation += 180; animator.SetFloat("Speed", -velocity.magnitude); }
					if (velocity.magnitude > 0.1) { }
					else { targetBodyRotation = fireRotation; }
				}
				bodyRotation = Mathf.LerpAngle(bodyRotation, targetBodyRotation, Time.deltaTime * 12);

				//transform.eulerAngles = new Vector3(0, bodyRotation, 0);
				//transform.rotation = Quaternion.AngleAxis(Manager.baseTilt, Camera.main.transform.right);
				//transform.rotation *= Quaternion.AngleAxis(bodyRotation, Vector3.up);
			}
		}
	}

	public void UpdateGraphics() {
		//if (currentGadget != null && currentGadget.graphic != null) { weaponGraphic.sprite = currentGadget.graphic; }
		//weaponGraphic.transform.localPosition = direction.normalized * 0.5f + Vector3.up;
		//weaponGraphic.transform.eulerAngles = new Vector3(0, Mathf.Atan2(-direction.z, direction.x) * Mathf.Rad2Deg, 0);

		/*
		if (weaponGraphic.transform.eulerAngles.y > 90 && weaponGraphic.transform.eulerAngles.y < 270) { weaponGraphic.flipY = true; }
		else { weaponGraphic.flipY = false; }

		int weaponOrder = 5;
		if (weaponGraphic.transform.eulerAngles.y > 0 && weaponGraphic.transform.eulerAngles.y < 180) { weaponOrder = -5; }
		if (characterGraphic) { weaponGraphic.sortingOrder = characterGraphic.sortingOrder + weaponOrder; }
		*/
	}

	public void TakeDamage(float dmg, bool makeInvincible = true) {
		if (invincibilityFrames > 0) { return; }
		health -= dmg;
		if (useInvincibilityFrames && makeInvincible) { invincibilityFrames = 0.5f; }
		if (health < 0) { health = 0; }

		if (health > 0) {
			// Manager.pauseTime = 0.2f;}
		}
		else {
			//StartCoroutine(Manager.instance.Hit(0.2f, 1f, 0.08f));
			//Manager.PlaySound(Manager.instance.deathSound, transform.position, 3f, 0.9f);
			Die();
		}
	}
	public void TakeDamage(float dmg, Vector3 pos, bool makeInvincible = true) {
		/*
		if (ai) {
			pos.y = transform.position.y;
			Vector3 _pos = (pos - transform.position);
			_pos.y = 0;
			//ai.Disturbance(_pos, dmg);
		}
		*/
		TakeDamage(dmg, makeInvincible);
	}

	public void Die() {
		isAlive = false;
		//StartCoroutine(Manager.instance.Hit(0.2f, 1f, 0.08f));
		//Manager.PlaySound(Manager.instance.deathSound, transform.position, 3f, 0.9f);
		animator.SetTrigger("Death");
	}

	public float CheckRelationship(Character c) {
		return 50;
	}
	public bool CheckHostile(Character c) {
		return false;
	}
	
	public void Dash(Vector3 direction) {
		bool chaining = false;
		/*
		if (dashCooldownSpent > 0 && dashCooldownSpent <= dashSweetSpot) {
			chaining = true;
			dashChain++;
			Manager.PlaySound(dashChainSound, transform.position);
		}
		*/
		if (isStunned && !chaining) { return; }

		//InterruptAttack();

		animator.SetTrigger("Dash Start");
		if (dashEffect) {
			GameObject eff = (GameObject)GameObject.Instantiate(dashEffect, transform.position, new Quaternion());
			Destroy(eff, 2);
		}
		movementType = Character.MovementType.Dashing;
		//Manager.PlaySound(dashSound, transform.position);
		velocity = Vector3.ClampMagnitude(direction.normalized * 36, 36);
		dashTimeSpent = 0;
	}

	public void Decelerate(float decel) {
		velocity += -velocity * Mathf.Clamp01(Time.deltaTime * decel);
	}

	void OnCollisionEnter(Collision col) {
		if (movementType == Character.MovementType.Dashing) {
			// Bonk!
			//EndDash();
			Shove(-velocity, velocity.magnitude);
		}
	}

	void Shove(Vector3 direction, float force) {
		// Depending on force and weight, push, fling, etc
	}

	void EndDash() {
		animator.SetTrigger("Dash End");
		dashTimeSpent = 0;
		dashCooldownSpent = 0.001f;
		movementType = Character.MovementType.Running;
		stunTimer += dashCooldown;
	}
}
