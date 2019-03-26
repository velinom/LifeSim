using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Singleton game manager, can be accessed from any other script
public class GameManager : MonoBehaviour {

	// Singleton instance, instantiated in Awake
	public static GameManager instance = null;

	// Used to procedurally generate the board
	public BoardManager boardScript;

	// SEED field that can be set by the player
	private int seed;
	private bool useSeed;

	// Called when the GameManager is instantiated for the first time. Initialize values
	// and sets up the singleton instance
	void Awake () {
		// set up instance
		if (instance == null)
			instance = this;
		else if (instance != this)
			Destroy(gameObject);

		DontDestroyOnLoad(gameObject);
	}

  // Initializes the game
	public void initGame() {
		// Initialize values and start the game
		boardScript = GetComponent<BoardManager>();
		boardScript.createScene(this.useSeed, this.seed);
	}

	// Setters and getters for the seed info
	public int getSeed() { return seed; }
	public void setSeed(int s) { this.seed = s; }

	public bool shouldUseSeed() { return useSeed; }
	public void setUseSeed(bool use) { this.useSeed = use; }
}
