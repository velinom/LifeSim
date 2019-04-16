using Models;
using System.Collections;
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
	public BaseAgent[] SHEEP;
	public BaseAgent[] WOLVES;
	public FormationController FORMATION;

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
	private List<BaseAgent> spawnedSheep;
	//private List<BaseAgent> spawnedWolves;

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
		this.spawnedSheep = new List<BaseAgent>();

		// Make sure that sheep smells update every so many seconds
		InvokeRepeating("UpdateSheepSmell", 1.0f, 2.0f);
	}

	// Called every so many seconds to propogate the sheep's smell through the board
	// as the sheep move arround. Calling is setup in the Awake method to happen a fixed
	// number of seconds
	private void UpdateSheepSmell() {
		StartCoroutine(updateSheepSmellThreaded());
	}

	// Threaded version of the update smell function that doesn't hog all the system 
	// resources
	IEnumerator updateSheepSmellThreaded() {
		// Begin by setting the sheep smell to 0 for the whole map.
		for (int i = 0; i < SIZE; i++) {
			for (int j = 0; j < SIZE; j++) {
			  smellArray[i, j].setSmellToZero(SmellType.MeatFood);
			}
		}

		// Need to copy list of sheep so that new sheep aren't added in the middel of the co-routine
		List<BaseAgent> sheepCopy = new List<BaseAgent>();
		foreach (BaseAgent sheep in this.spawnedSheep) {
			if (sheep != null) sheepCopy.Add(sheep);
		}

		// Now for every sheep, propogate its smell through the map
		foreach (BaseAgent sheep in sheepCopy) {
			yield return null;
			if (sheep == null) continue;
			Vector2 sheepCell = sheep.getCurrentCell();
			List<BoardManager.TileType> impassable =  new List<BoardManager.TileType> {
				BoardManager.TileType.High, BoardManager.TileType.Water
			};
			boardScript.propagateSmellFromRoot(sheepCell, SmellType.MeatFood, impassable);
		}
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

		BaseAgent toSpawn = null;
		if (name == "sheep") {
			toSpawn = SHEEP[Random.Range(0, SHEEP.Length)];
			BaseAgent spawned = Instantiate(toSpawn, location, Quaternion.identity);
			spawned.transform.SetParent(animalHolder);
			this.spawnedSheep.Add(spawned);
		} else if (name == "wolf") {
			toSpawn = WOLVES[Random.Range(0, WOLVES.Length)];
			BaseAgent spawned = Instantiate(toSpawn, location, Quaternion.identity);
			spawned.transform.SetParent(animalHolder);
		} else {
			Debug.Log("Attempted to spawn unrecognized animal");
		}
	}

	// Spawn a pack of wolves at the given location
	public void spawnWolfPack(Vector2 location) {
		FormationController formationController = Instantiate(FORMATION, location, Quaternion.identity);
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
	public List<BaseAgent> getSpawnedSheep() { return this.spawnedSheep; }
}
