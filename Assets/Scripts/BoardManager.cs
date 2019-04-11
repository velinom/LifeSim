using Models;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Random = UnityEngine.Random;

// Procedurally generates and renders the game board (Three different elevations)
public class BoardManager : MonoBehaviour {
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
	public enum TileType { Low, Medium, High, Water }

	// The different food that can be spawned in the world
	public enum Food { None, Tree, Bush }

	// Parent objects that hold spawned children, keeps the object hierarchy neat
	private Transform boardHolder;
	private Transform foodHolder;
	public Canvas textHolder; // Canvas for rendering onto for debugging

	// 2D array of tyle-types that gets randomly generated
	private TileType[, ] boardArray;

	// 2D array of objects that get randomly placed on the board
	private Food[, ] foodArray;

	// 2D array of smell objects representing the smell at each cell
	private Smell[, ] smellArray;

	// Hold x, y, coordinates for the trees, water and bushes 
	// Used to initially propogate smells, but not used in game
	private List<Vector2> treeLocations;
	private List<Vector2> bushLocations;
	private List<Vector2> waterLocations;

  // Initializes the boardArray using random methods to set the elevation
	// type at each position on the board.
	private void setupBoard() {
		boardArray = new TileType[GameManager.SIZE, GameManager.SIZE];

		// Begin by making every tile Low elevation
		for (int x = 0; x < GameManager.SIZE; x++) {
			for (int y = 0; y < GameManager.SIZE; y++) {
				boardArray[x, y] = TileType.Low;
			}
		}

		// Now place the middle tiles, calculate a number of random roots from the 
		// set range to use
		int numberOfRoots = Random.Range(NUM_HILLS.minimum, NUM_HILLS.maximum);
		List<Vector2> roots = new List<Vector2>();
		for (int i = 0; i < numberOfRoots; i++) {
			Vector2 ithRoot = new Vector2(Random.Range(0, GameManager.SIZE), Random.Range(0, GameManager.SIZE));
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
				waterRoot = new Vector2(Random.Range(0, GameManager.SIZE), Random.Range(0, GameManager.SIZE));
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
		int tilesToPlace = (int)(GameManager.SIZE * GameManager.SIZE * coveragePercent);
		int placedTiles = 0;

		// Open list of tiles to set and then propogate, init with roots
		Queue<Vector2> openList = new Queue<Vector2>();
		foreach (Vector2 root in roots ) {
			openList.Enqueue(root);
		}

		// In a loop, place tiles (starting at the roots) until queue is empty
		// or we have reached tilesToPlace
		waterLocations = new List<Vector2>();
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
			if (type == TileType.Water) waterLocations.Add(new Vector2(curX, curY));

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
			if (curY < GameManager.SIZE - 1 && boardArray[curX, curY + 1] != type) {
				if (Random.value < expansionChance) {
					openList.Enqueue(new Vector2(curX, curY + 1));
				}
			}
			// Adding the right neighbor
			if (curX < GameManager.SIZE - 1 && boardArray[curX + 1, curY] != type) {
				if (Random.value < expansionChance) {
					openList.Enqueue(new Vector2(curX + 1, curY));
				}
			}
		}
	}
	
  // Uses the randomly generated boardArray to place random tiles of the correct type
	// on the board by instantiating it and putting it in the right place.
	private void createBoard() {
		boardHolder = new GameObject("Board").transform;

		for (int x = -1; x < GameManager.SIZE + 1; x++) {
			for (int y = -1; y < GameManager.SIZE + 1; y++) {
				// If we are on the edge of the map, spawn a high-elevation tile to
				// enclose the map with impassible objects
				if (x == -1 || x == GameManager.SIZE || y == -1 || y == GameManager.SIZE) {
					GameObject edgeObject = HIGH_ELEVATION_TILES[Random.Range(0, HIGH_ELEVATION_TILES.Length)];
					GameObject edgeInstance = Instantiate(
					  edgeObject, new Vector3(x * GameManager.CELL_SIZE,
					  y * GameManager.CELL_SIZE, 0f), Quaternion.identity) as GameObject;
				  edgeInstance.transform.SetParent(boardHolder);
					continue;
				}

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
					toInstantiate, new Vector3(x * GameManager.CELL_SIZE,
					 y * GameManager.CELL_SIZE, 0f), Quaternion.identity) as GameObject;
				instance.transform.SetParent(boardHolder);
			}
		}
	}

	// Populates the map with objects including bushes and trees for food
	private void setupObjects() {
		foodArray = new Food[GameManager.SIZE, GameManager.SIZE];

		// Begin with trees which are food for birds
		int numTrees = Random.Range(NUM_TREES.minimum, NUM_TREES.maximum);
		List<TileType> allowedTiles = new List<TileType> { TileType.Low, TileType.Medium }; 
		spawnObjectsAtRandom(Food.Tree, numTrees, allowedTiles);

		// Place Bushes which are food for land animals
		int numBushes = Random.Range(NUM_BUSHES.minimum, NUM_BUSHES.maximum);
		spawnObjectsAtRandom(Food.Bush, numBushes, allowedTiles);

		// Loop over object array and store tree / bush locations
		treeLocations = new List<Vector2>();
		bushLocations = new List<Vector2>();
		for (int x = 0; x < GameManager.SIZE; x++) {
			for (int y = 0; y < GameManager.SIZE; y++) {
				if (foodArray[x, y] == Food.Tree) {
					treeLocations.Add(new Vector2(x, y));
				} else if (foodArray[x, y] == Food.Bush) {
					bushLocations.Add(new Vector2(x, y));
				}
			}
		}
	}

	// Spawns the given number of tiles at random, only placing them on allowed tiles
	private void spawnObjectsAtRandom(Food type, int numToSpawn, List<TileType> allowed) {
		while(numToSpawn > 0) {
			bool placed = false;
			while (!placed) {
				int x = Random.Range(0, GameManager.SIZE);
				int y = Random.Range(0, GameManager.SIZE);

				// Begin by checking that there isn't an object there already
				if (foodArray[x, y] != Food.None) continue;

				// Make sure the the random location is allowed
				if (!allowed.Contains(boardArray[x, y])) continue;

				// If we have reached this point we will set the object in the array
				foodArray[x, y] = type;
				placed = true;
			} 

			numToSpawn--;
		}
	}

	// Uses the generated object array to instantiate the corresponding game 
	// objects in the scene
	private void createObjects() {
		foodHolder = new GameObject("Food").transform;

		for (int x = 0; x < GameManager.SIZE; x++) {
			for (int y = 0; y < GameManager.SIZE; y++) {
				GameObject toInstantiate = null;
				Food currentType = foodArray[x, y];

				switch(currentType) {
					case Food.Tree: {
            toInstantiate = TREES[Random.Range(0, TREES.Length)];
						break;
					}
					case Food.Bush: {
						toInstantiate = BUSHES[Random.Range(0, BUSHES.Length)];
						break;
					}
					case Food.None: break;
					default: break;
				}

				// Instantiate the right tyle object at the location
				if (toInstantiate != null) {
					GameObject instance = Instantiate(
						toInstantiate, new Vector3(x * GameManager.CELL_SIZE,
						y * GameManager.CELL_SIZE, 0f), Quaternion.identity) as GameObject;
					instance.transform.SetParent(foodHolder);
				}
			}
		}
	}

	// Need to propagate each smell from its source (potentially ignoring some terain types)
	// to determine how the smell fills the board. This is expensive but (mostly) only done
	// once at the beginning of the game.
	private void propagateSmells() {
		// Setup the smell-gird (A 2x2 array of Smell objects)
		this.smellArray = new Smell[GameManager.SIZE, GameManager.SIZE];

		// Begin by propagating the trees
		// Loop over all tiles and determine the smell from each tree at that tile
		// This is the "easy" one since birds will smell trees and don't need to worry
		// about the smell being blocked by high-elevation or water.
		for (int x = 0; x < GameManager.SIZE; x++) {
			for (int y = 0; y < GameManager.SIZE; y++) {
				// Loop over each tree and add the smell from that tree to the Smell object
				// at the current tile
				Smell curLocSmell = new Smell();
				foreach (Vector2 loc in treeLocations) {
					float deltaX = Mathf.Abs(loc.x - x);
					float deltaY = Mathf.Abs(loc.y - y);
					double distance = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);

					if (distance < 0.1) {
						curLocSmell.addToSmell(SmellType.TreeFood, 1);
					} else {
						curLocSmell.addToSmell(SmellType.TreeFood, 1.0 / (distance * distance));
					}
				}
				smellArray[x, y] = curLocSmell;
			}
		}

		// Now for bushes, the smell must "stop" at high-elevation and water tiles
		// this means we have to acutally propagate the smell through the environment 
		// this can be costly but is necessary for good behavior from the agents.
		List<TileType> impassable = new List<TileType> { TileType.High, TileType.Water };
		foreach (Vector2 bushLoc in this.bushLocations) {
			propagateSmellFromRoot(bushLoc, SmellType.GroundFood, impassable);
		}

		// For wat the smell should stop when passing through mountains but not on other
		// water tiles
		impassable = new List<TileType> { TileType.High };
		foreach (Vector2 waterLoc in this.waterLocations) {
			propagateSmellFromRoot(waterLoc, SmellType.Water, impassable);
		}
	}

	// Propogate the smell from a single object radiating the smell of the given
	// type throughout the whole map. Allows for the given smell not to be able
	// to pass through tile-types from the given "impassable" list
	public void propagateSmellFromRoot(Vector2 root, SmellType type, List<TileType> impassable) {
		Queue<PropagatingSmell> openList = new Queue<PropagatingSmell>();
		HashSet<Vector2> closedList = new HashSet<Vector2>();
		openList.Enqueue(new PropagatingSmell(1, root));

		while (openList.Count > 0) {
			PropagatingSmell current = openList.Dequeue();

			// If we've already reached this cell, move on
			if (closedList.Contains(current.location)) continue;

			// Set the smell in this cell, add it to closed list
			int curX = (int)current.location.x;
			int curY = (int)current.location.y;
			float smellValue = 1.0f / (current.cellsAway * current.cellsAway);
			//Debug.Log("Setting at : " + curX + ", " + curY);
			smellArray[curX, curY].addToSmell(type, smellValue);
			closedList.Add(new Vector2(curX, curY));

			// Add the neighbors of this cell if they aren't in the impassable list
			// Up Neieghbor
			if (curY < GameManager.SIZE - 1) {
				TileType neighborType = boardArray[curX, curY +1];
				if (!impassable.Contains(neighborType)) {
					openList.Enqueue(new PropagatingSmell(current.cellsAway + 1,
						new Vector2(curX, curY + 1)));
				}
			}
			// Right Neighbor
			if (curX < GameManager.SIZE - 1) {
				TileType neighborType = boardArray[curX + 1, curY];
				if (!impassable.Contains(neighborType)) {
					openList.Enqueue(new PropagatingSmell(current.cellsAway + 1,
						new Vector2(curX + 1, curY)));
				}
			}
			// Down Neighbor
			if (curY > 0) {
				TileType neighborType = boardArray[curX, curY - 1];
				if (!impassable.Contains(neighborType)) {
					openList.Enqueue(new PropagatingSmell(current.cellsAway + 1, 
						new Vector2(curX, curY - 1)));
				}
			}
			// Left Neighbor
			if (curX > 0) {
				TileType neighborType = boardArray[curX - 1, curY];
				if (!impassable.Contains(neighborType)) {
					openList.Enqueue(new PropagatingSmell(current.cellsAway + 1, 
						new Vector2(curX - 1, curY)));
				}
			}
		}
	}

	// Helper / debugging method to display the value for the given type of smell
	// at each cell on the board. Shouldn't be used in actual game
	private void displaySmells(SmellType type) {
		// Render on the canvas the smell values
		for (int x = 0; x < GameManager.SIZE; x++) {
			for (int y = 0; y < GameManager.SIZE; y++) {
				GameObject newGO = new GameObject("Some Text");
				newGO.transform.SetParent(textHolder.transform);
				Text newText = newGO.AddComponent<Text>();
				newText.text = (smellArray[x, y].getSmell(type)).ToString();
				newGO.transform.localScale = new Vector3(0.02f, 0.02f, 1);
				Font ArialFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
				newText.font = ArialFont;
				newText.material = ArialFont.material;
				newText.color = new Color(0, 0, 0);
				
				RectTransform textTransform = newText.GetComponent<RectTransform>();
				textTransform.sizeDelta = new Vector2(36, 25);
				textTransform.anchorMin = new Vector2(0, 0);
				textTransform.anchorMax = new Vector2(0, 0);
				textTransform.pivot = new Vector2(0, 0);
				newText.transform.position = new Vector3(x * GameManager.CELL_SIZE - 0.05f,
				y * GameManager.CELL_SIZE - 0.05f, 0);
			}
		}
	}

	// To be called by the game manager, randomly makes the board
	// and instantiates the tiles in their place
	public void createScene(bool useSeed, int seed) {
		if (useSeed) Random.InitState(seed);

		// Setup the water and land tiles, then render them
		setupBoard();
		createBoard();

		// Setup the tree and bush tiles, then render them
		setupObjects();
		createObjects();

		// Setup the smells and store them for easy access in game
		propagateSmells();
		//displaySmells(SmellType.GroundFood);
	}

	/*
	 * GETTERS SO THE GAME MANAGER CAN GET ARRAYS
	 */
	public TileType[, ] getBoardArray() {
		return boardArray;
	}

	public Food[, ] getFoodArray() {
		return foodArray;
	}

	public Smell[, ] getSmellArray() {
		return smellArray;
	}
}
