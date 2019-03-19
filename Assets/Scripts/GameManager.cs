using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

	public BoardManager boardScript;

	// Called when the GameManager is instantiated. Initialize values
	void Awake () {
		boardScript = GetComponent<BoardManager>();
		initGame();
	}

  // Initializes the game
	private void initGame() {
		boardScript.createScene();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
