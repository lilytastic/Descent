using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Traits are: Kindness, Honesty, Pragmatism, Pacifism, Cleverness, Directness, Power, Selflessness, Endurance, Humility, Piety

public class Entity : ScriptableObject {
	public string nameDisplayed = "[Default]";

	public string description = "";

	public string[] likes = new string[0];
	public string[] dislikes = new string[0];

	public string[] trusts = new string[0];
	public string[] distrusts = new string[0];

	public string[] respects = new string[0];
	public string[] disrespects = new string[0];

	public Entity() { }
}