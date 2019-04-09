using Models;
using System.Collections.Generic;
using UnityEngine;

// Singleton game manager, can be accessed from any other script
public class GameManager : MonoBehaviour {

	// Singleton instance, instantiated in Awake
	public static GameManager instance = null;

	// The number of tiles on the board
	public static int SIZE = 50;

	// The size of each tile in the board, in hundreds of px
	public static float CELL_SIZE = 1.2f;

	// Game Object prefabs passed in through unity to spawn when needed
	public GameObject[] SHEEP;
	public GameObject[] WOLVES;

	// Transform to hold all the animals that the game manager spawns
	private Transform animalHolder; 

	// Used to procedurally generate the board
	private BoardManager boardScript;

	// SEED field that can be set by the player
	private int seed;
	private bool useSeed;

	// Board information set from the BoardManager
	private Smell[, ] smellArray;
	private BoardManager.Food[, ] foodArray;
	private BoardManager.TileType[, ] boardArray;

	// Animal information, animals are spawned here
	private List<GameObject> spawnedSheep;

	// Called when the GameManager is instantiated for the first time. Initialize values
	// and sets up the singleton instance
	void Awake () {
		// set up singleton instance
		if (instance == null)
			instance = this;
		else if (instance != this)
			Destroy(gameObject);
		DontDestroyOnLoad(gameObject);

		// Initialize
		this.spawnedSheep = new List<GameObject>();
	}

  // Initializes the game
	public void initGame() {
		// Initialize values and start the game
		boardScript = GetComponent<BoardManager>();
		boardScript.createScene(this.useSeed, this.seed);

		// Get the board values form the board manager
		this.boardArray = boardScript.getBoardArray();
		this.smellArray = boardScript.getSmellArray();
		this.foodArray = boardScript.getFoodArray();
	}

	// Spawn an animal of the given name at the given location
	public void spawnAnimalAtLocation(string name, Vector2 location) {
		if (animalHolder == null) animalHolder = new GameObject("Animals").transform;

		GameObject toSpawn = null;
		if (name == "sheep") {
			toSpawn = SHEEP[Random.Range(0, SHEEP.Length)];
			this.spawnedSheep.Add(toSpawn);
		} else if (name == "wolf") {
			toSpawn = WOLVES[Random.Range(0, WOLVES.Length)];
		} else {
			Debug.Log("Attempted to spawn unrecognized animal");
		}

		GameObject spawned = Instantiate(toSpawn, location, Quaternion.identity) as GameObject;
		spawned.transform.SetParent(animalHolder);
	}

	/*
	 * GETTERS AND SETTERS
	 */
	// For the seed that the user can set 
	public int getSeed() { return seed; }
	public void setSeed(int s) { this.seed = s; }

	public bool shouldUseSeed() { return useSeed; }
	public void setUseSeed(bool use) { this.useSeed = use; }

	// For the board info
	public Smell[, ] getSmellArray() { return this.smellArray; }
	public BoardManager.Food[, ] getFoodArray() { return this.foodArray; }
	public BoardManager.TileType[, ] getBoardArray() { return this.boardArray; }
}
