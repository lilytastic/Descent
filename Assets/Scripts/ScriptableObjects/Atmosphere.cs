using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Atmosphere : ScriptableObject {
	public Color backgroundColor = Color.grey;
	public GameObject particles = null;
	public AudioClip[] ambience = new AudioClip[0];

	public Atmosphere() { }
}