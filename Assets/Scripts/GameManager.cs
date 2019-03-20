using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Singleton game manager, can be accessed from any other script
public class GameManager : MonoBehaviour {

	// Singleton instance, instantiated in Awake
	public static GameManager instance = null;

	// Used to procedurally generate the board
	public BoardManager boardScript;

	// Called when the GameManager is instantiated for the first time. Initialize values
	// and sets up the singleton instance
	void Awake () {
		// set up instance
		if (instance == null)
			instance = this;
		else if (instance != this)
			Destroy(gameObject);

		DontDestroyOnLoad(gameObject);

		// Initialize values and start the game
		boardScript = GetComponent<BoardManager>();
		initGame();
	}

  // Initializes the game
	private void initGame() {
		boardScript.createScene(false, 1234);
	}
}
