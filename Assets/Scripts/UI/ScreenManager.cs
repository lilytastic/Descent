using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenManager : MonoBehaviour {

	public GameManager.ScreenState state = GameManager.ScreenState.Main;

	public void ChangeState (string _state) {
		state = (GameManager.ScreenState)System.Enum.Parse(typeof(GameManager.ScreenState), _state);
	}
}
