using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World {
	public string name = "";

	public Dictionary<string,Room> rooms = new Dictionary<string,Room>();
}

public class SaveFile {
	public string storyState = "";
	public string lastSaved = "";
	public float playtime = 0;

	public string bgm = "";
	public string room = "";

	public float posX = 0;
	public float posY = 0;
	public float posZ = 0;

	public string currentState = "gameplay";

	public string ToJson() {
		return JsonUtility.ToJson(this);
	}

	public SaveFile() {
	}
}

public class Room {
	Vector3 coord = new Vector3();

	public Room() { }
	public Room(Vector3 _c) { coord = _c; }
}

public class Trait {
	public string name = "";
	public Trait() { }
}

public class Word {
	public string printed = "";
	public Phoneme[] phonemes = new Phoneme[0];
	public string endsWith = "";

	public Word() { }
	public Word(Phoneme[] p) { phonemes = p; }
	public Word(Phoneme[] p, string w) { phonemes = p; printed = w; }
}

[System.Serializable]
public class Language {
	public List<string> dictionary = new List<string>();
	public List<Phoneme> phonemes = new List<Phoneme>();

	public string PrintWord(Phoneme[] p) {
		string woo = "";
		foreach (Phoneme _p in p) {
			woo += _p.printed;
		}
		return woo;
	}

	public Phoneme GetConsonant() {
		List<Phoneme> consonants = new List<Phoneme>();
		foreach (Phoneme p in phonemes) { if (!p.vowel) { consonants.Add(p); } }
		return consonants[Random.Range(0, consonants.Count)];
	}
	public Phoneme GetVowel() {
		List<Phoneme> vowels = new List<Phoneme>();
		foreach (Phoneme p in phonemes) { if (p.vowel) { vowels.Add(p); } }
		return vowels[Random.Range(0, vowels.Count)];
	}

	public Language() { }
}

[System.Serializable]
public class VoicePack {
	public AudioClip ah = null;
	public AudioClip e = null;
	public AudioClip ee = null;
	public AudioClip oo = null;
	public AudioClip d = null;
	public AudioClip l = null;
}

[System.Serializable]
public class Phoneme {
	public string sound;
	public string printed;
	public bool vowel = false;
	public Phoneme() { }
}