using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// Procedurally generates and renders the game board (Three different elevations)
public class BoardManager : MonoBehaviour {

	// Size of the game board, set to public so they can be modified in Unity
	public int SIZE = 50;

	// In hundreds of px for use with unity placing.
	public float CELL_SIZE;

	// Min and max number of different things to spawn in game
	public Count NUM_HILLS;  // Number of hill root nodes to spawn
	public Count NUM_PONDS;  // Number of water root nodes to spawn
	public Count NUM_TREES;
	public Count NUM_BUSHES;

	// For the random generation, percentage that each tile should take up
	// and the chance of expanding any given cell on the front
	public double MID_ELEVATION_PERCENT;
	public double MID_EXPANSION_CHANCE;
	public double HIGH_ELEVATION_PERCENT;
	public double HIGH_EXPANSION_CHANCE;
	public double WATER_PERCENT;
	public double WATER_EXPANSION_CHANCE;

	// Floor tiles that are passed in through Unity
	public GameObject[] LOW_ELEVATION_TILES;
	public GameObject[] MID_ELEVATION_TILES;
	public GameObject[] HIGH_ELEVATION_TILES;
	public GameObject[] WATER_TILES;
	public GameObject[] TREES;
	public GameObject[] BUSHES;

	// The three types of elevation tiles and water
	private enum TileType { Low, Medium, High, Water }

	// The different objects that can be spawned in the world
	private enum Object { None, Tree, Bush }

	// Parent objects that hold spawned children, keeps the object hierarchy neat
	private Transform boardHolder;
	private Transform foodHolder;

	// 2D array of tyle-types that gets randomly generated
	private TileType[, ] boardArray;

	// 2D array of objects that get randomly placed on the board
	private Object[, ] objectArray;

  // Initializes the boardArray using random methods to set the elevation
	// type at each position on the board.
	private void setupBoard() {
		boardArray = new TileType[SIZE, SIZE];

		// Begin by making every tile Low elevation
		for (int x = 0; x < SIZE; x++) {
			for (int y = 0; y < SIZE; y++) {
				boardArray[x, y] = TileType.Low;
			}
		}

		// Now place the middle tiles, calculate a number of random roots from the 
		// set range to use
		int numberOfRoots = Random.Range(NUM_HILLS.minimum, NUM_HILLS.maximum);
		List<Vector2> roots = new List<Vector2>();
		for (int i = 0; i < numberOfRoots; i++) {
			Vector2 ithRoot = new Vector2(Random.Range(0, SIZE), Random.Range(0, SIZE));
			roots.Add(ithRoot);
		}
		propagateRootTiles(roots, MID_ELEVATION_PERCENT, MID_EXPANSION_CHANCE, TileType.Medium);

		// Now place the high elevation tiles starting at the same roots
		propagateRootTiles(roots, HIGH_ELEVATION_PERCENT, HIGH_EXPANSION_CHANCE, TileType.High);

		// Finally place the water tiles
		roots = new List<Vector2>();
		int numWaterRoots = Random.Range(NUM_PONDS.minimum, NUM_PONDS.maximum);
		for (int i = 0; i < numWaterRoots; i++) {
			Vector2 waterRoot = new Vector2(0, 0);
			bool validRoot = false;
			while (!validRoot) {
				waterRoot = new Vector2(Random.Range(0, SIZE), Random.Range(0, SIZE));
				validRoot = boardArray[(int)waterRoot.x, (int)waterRoot.y] != TileType.High;
			}
			roots.Add(waterRoot);
		}
		propagateRootTiles(roots, WATER_PERCENT, WATER_EXPANSION_CHANCE, TileType.Water);
	}

	// Takes in an array of root tiles, a percent of the board to cover, an 
	// expansion chance for any given tile, and the type type
	private void propagateRootTiles(List<Vector2> roots, double coveragePercent,
	                                double expansionChance, TileType type) {
		// Start by calculating how many tiles of this type to place
		int tilesToPlace = (int)(SIZE * SIZE * coveragePercent);
		int placedTiles = 0;

		// Open list of tiles to set and then propogate, init with roots
		Queue<Vector2> openList = new Queue<Vector2>();
		foreach (Vector2 root in roots ) {
			openList.Enqueue(root);
		}

		// In a loop, place tiles (starting at the roots) until queue is empty
		// or we have reached tilesToPlace
		while (placedTiles < tilesToPlace && openList.Count > 0) {
			// Pop off the queue
			Vector2 currentTile = openList.Dequeue();
			int curX = (int)currentTile.x;
			int curY = (int)currentTile.y;

			// Don't set tiles twice
			if (boardArray[curX, curY] == type) continue;
			// Don't let water enter high elevation
			if (type == TileType.Water && boardArray[curX, curY] == TileType.High) continue;

			// Set the tile to the desired type
			boardArray[curX, curY] = type;
			placedTiles++;

			// Add neighbors to Queue if they aren't the same type
			// Adding the down neighbor
			if (curY > 0 && boardArray[curX, curY - 1] != type) {
				if (Random.value < expansionChance) {
					openList.Enqueue(new Vector2(curX, curY - 1));
				}
			}
			// Adding the left neighbor
			if (curX > 0 && boardArray[curX - 1, curY] != type) {
				if (Random.value < expansionChance) {
					openList.Enqueue(new Vector2(curX - 1, curY));
				}
			}
			// Adding the top neighbor
			if (curY < SIZE - 1 && boardArray[curX, curY + 1] != type) {
				if (Random.value < expansionChance) {
					openList.Enqueue(new Vector2(curX, curY + 1));
				}
			}
			// Adding the right neighbor
			if (curX < SIZE - 1 && boardArray[curX + 1, curY] != type) {
				if (Random.value < expansionChance) {
					openList.Enqueue(new Vector2(curX + 1, curY));
				}
			}
		}
	}
	
  // Uses the randomly generaed boardArray to place random tiles of the correct type
	// on the board by instantiating it and putting it in the right place.
	private void createBoard() {
		boardHolder = new GameObject("Board").transform;

		for (int x = 0; x < SIZE; x++) {
			for (int y = 0; y < SIZE; y++) {
				GameObject toInstantiate = null;
				TileType currentType = boardArray[x, y];

				switch(currentType) {
					case TileType.Low: {
            toInstantiate = LOW_ELEVATION_TILES[Random.Range(0, LOW_ELEVATION_TILES.Length)];
						break;
					}
					case TileType.Medium: {
						toInstantiate = MID_ELEVATION_TILES[Random.Range(0, MID_ELEVATION_TILES.Length)];
						break;
					}
					case TileType.High: {
						toInstantiate = HIGH_ELEVATION_TILES[Random.Range(0, HIGH_ELEVATION_TILES.Length)];
						break;
					}
					case TileType.Water: {
						toInstantiate = WATER_TILES[Random.Range(0, WATER_TILES.Length)];
						break;
					}
					default: break;
				}

				// Instantiate the right tyle object at the location
				GameObject instance = Instantiate(
					toInstantiate, new Vector3(x * CELL_SIZE, y * CELL_SIZE, 0f), Quaternion.identity) as GameObject;
				instance.transform.SetParent(boardHolder);
			}
		}
	}

	// Populates the map with objects including bushes and trees for food
	private void setupObjects() {
		objectArray = new Object[SIZE, SIZE];

		// Begin with trees which are food for birds
		int numTrees = Random.Range(NUM_TREES.minimum, NUM_TREES.maximum);
		List<TileType> allowedTiles = new List<TileType> { TileType.Low, TileType.Medium }; 
		spawnObjectsAtRandom(Object.Tree, numTrees, allowedTiles);

		// Place Bushes which are food for land animals
		int numBushes = Random.Range(NUM_BUSHES.minimum, NUM_BUSHES.maximum);
		spawnObjectsAtRandom(Object.Bush, numBushes, allowedTiles);

	}

	// Spawns the given number of tiles at random, only placing them on allowed tiles
	private void spawnObjectsAtRandom(Object type, int numToSpawn, List<TileType> allowed) {
		while(numToSpawn > 0) {
			bool placed = false;
			while (!placed) {
				int x = Random.Range(0, SIZE);
				int y = Random.Range(0, SIZE);

				// Begin by checking that there isn't an object there already
				if (objectArray[x, y] != Object.None) continue;

				// Make sure the the random location is allowed
				if (!allowed.Contains(boardArray[x, y])) continue;

				// If we have reached this point we will set the object in the array
				objectArray[x, y] = type;
				placed = true;
			} 

			numToSpawn--;
		}
	}

	// Uses the generated object array to instantiate the corresponding game 
	// objects in the scene
	private void createObjects() {
		foodHolder = new GameObject("Food").transform;

		for (int x = 0; x < SIZE; x++) {
			for (int y = 0; y < SIZE; y++) {
				GameObject toInstantiate = null;
				Object currentType = objectArray[x, y];

				switch(currentType) {
					case Object.Tree: {
            toInstantiate = TREES[Random.Range(0, TREES.Length)];
						break;
					}
					case Object.Bush: {
						toInstantiate = BUSHES[Random.Range(0, BUSHES.Length)];
						break;
					}
					case Object.None: break;
					default: break;
				}

				// Instantiate the right tyle object at the location
				if (toInstantiate != null) {
					GameObject instance = Instantiate(
						toInstantiate, new Vector3(x * CELL_SIZE, y * CELL_SIZE, 0f), Quaternion.identity) as GameObject;
					instance.transform.SetParent(foodHolder);
				}
			}
		}
	}

	// To be called by the game manager, randomly makes the board
	// and instantiates the tiles in their place
	public void createScene(bool useSeed, int seed) {
		if (useSeed) Random.InitState(seed);

		setupBoard();
		createBoard();
		setupObjects();
		createObjects();
	}
}
