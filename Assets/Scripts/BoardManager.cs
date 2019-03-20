﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// Procedurally generates and renders the game board (Three different elevations)
public class BoardManager : MonoBehaviour {

	// Size of the game board, set to public so they can be modified in Unity
	public int SIZE = 50;

	// In hundreds of px for use with unity placing.
	public float CELL_SIZE;

	// Min and max number of root hill nodes to expand
	public int MIN_ROOTS;
	public int MAX_ROOTS;

	// Min and max number of ponds to spawn in the map
	public int MIN_PONDS;
	public int MAX_PONDS;

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

	// The three types of elevation tiles
	private enum TileType { Low, Medium, High, Water }

	// Holds all the objects spawned into the board
	private Transform boardHolder;

	// 2D array of tyle-types that gets randomly generated
	private TileType[, ] boardArray;

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

		// Now place the middle tiles, start by calculating how many to place and
		// adding a random range of root tiles to the queue
		int tilesToPlace = (int)(SIZE * SIZE * MID_ELEVATION_PERCENT);
		int placedTiles = 0;
		int numberOfRoots = Random.Range(MIN_ROOTS, MAX_ROOTS);
		List<Vector2> roots = new List<Vector2>();
		Queue<Vector2> openList = new Queue<Vector2>();
		for (int i = 0; i < numberOfRoots; i++) {
			Vector2 ithRoot = new Vector2(Random.Range(0, SIZE), Random.Range(0, SIZE));
			roots.Add(ithRoot);
			openList.Enqueue(ithRoot);
		}

		// In a loop, place tiles until queue is empty or we have reached tilesToPlace
		while (placedTiles < tilesToPlace && openList.Count > 0) {
			// Pop off the queue and add make the tile med-elevation
			Vector2 currentTile = openList.Dequeue();
			int curX = (int)currentTile.x;
			int curY = (int)currentTile.y;
			if (boardArray[curX, curY] == TileType.Medium) continue;
			boardArray[curX, curY] = TileType.Medium;
			placedTiles++;

			// Add neighbors to Queue if they aren't mid with a percent chance
			// Adding the down neighbor
			if (curY > 0 && boardArray[curX, curY - 1] != TileType.Medium) {
				if (Random.value < MID_EXPANSION_CHANCE) {
					openList.Enqueue(new Vector2(curX, curY - 1));
				}
			}
			// Adding the left neighbor
			if (curX > 0 && boardArray[curX - 1, curY] != TileType.Medium) {
				if (Random.value < MID_EXPANSION_CHANCE) {
					openList.Enqueue(new Vector2(curX - 1, curY));
				}
			}
			// Adding the top neighbor
			if (curY < SIZE - 1 && boardArray[curX, curY + 1] != TileType.Medium) {
				if (Random.value < MID_EXPANSION_CHANCE) {
					openList.Enqueue(new Vector2(curX, curY + 1));
				}
			}
			// Adding the right neighbor
			if (curX < SIZE - 1 && boardArray[curX + 1, curY] != TileType.Medium) {
				if (Random.value < MID_EXPANSION_CHANCE) {
					openList.Enqueue(new Vector2(curX + 1, curY));
				}
			}
		}

		// Now place the high elevation tiles starting at the same roots
		openList.Clear();
		foreach (Vector2 root in roots) {
			openList.Enqueue(root);
		}
		tilesToPlace = (int)(SIZE * SIZE * HIGH_ELEVATION_PERCENT);
		placedTiles = 0;
		while(placedTiles < tilesToPlace && openList.Count > 0) {
			// Pop off the queue and add make the tile high-elevation
			Vector2 currentTile = openList.Dequeue();
			int curX = (int)currentTile.x;
			int curY = (int)currentTile.y;
			if (boardArray[curX, curY] == TileType.High) continue;
			boardArray[curX, curY] = TileType.High;
			placedTiles++;

			// Add neighbors to Queue if they aren't high with a percent chance
			// Adding the down neighbor
			if (curY > 0 && boardArray[curX, curY - 1] != TileType.High) {
				if (Random.value < HIGH_EXPANSION_CHANCE) {
					openList.Enqueue(new Vector2(curX, curY - 1));
				}
			}
			// Adding the left neighbor
			if (curX > 0 && boardArray[curX - 1, curY] != TileType.High) {
				if (Random.value < HIGH_EXPANSION_CHANCE) {
					openList.Enqueue(new Vector2(curX - 1, curY));
				}
			}
			// Adding the top neighbor
			if (curY < SIZE - 1 && boardArray[curX, curY + 1] != TileType.High) {
				if (Random.value < HIGH_EXPANSION_CHANCE) {
					openList.Enqueue(new Vector2(curX, curY + 1));
				}
			}
			// Adding the right neighbor
			if (curX < SIZE - 1 && boardArray[curX + 1, curY] != TileType.High) {
				if (Random.value < HIGH_EXPANSION_CHANCE) {
					openList.Enqueue(new Vector2(curX + 1, curY));
				}
			}
		}

		// Finally place the water tiles
		int waterRoots = Random.Range(MIN_PONDS, MAX_PONDS);
		tilesToPlace = (int)(SIZE * SIZE * WATER_PERCENT);
		placedTiles = 0;
		openList.Clear();
		for (int i = 0; i < waterRoots; i++) {
			Vector2 waterRoot = new Vector2(0, 0);
			bool validRoot = false;
			while (!validRoot) {
				waterRoot = new Vector2(Random.Range(0, SIZE), Random.Range(0, SIZE));
				validRoot = boardArray[(int)waterRoot.x, (int)waterRoot.y] != TileType.High;
			}
			openList.Enqueue(waterRoot);
		}

		// In a loop, place tiles until queue is empty or we have reached tilesToPlace
		while (placedTiles < tilesToPlace && openList.Count > 0) {
			// Pop off the queue and add make the tile med-elevation
			Vector2 currentTile = openList.Dequeue();
			int curX = (int)currentTile.x;
			int curY = (int)currentTile.y;
			if (boardArray[curX, curY] == TileType.Water) continue;
			boardArray[curX, curY] = TileType.Water;
			placedTiles++;

			// Add neighbors to Queue if they aren't mid with a percent chance
			// Adding the down neighbor
			if (curY > 0 && boardArray[curX, curY - 1] != TileType.Water) {
				if (Random.value < WATER_EXPANSION_CHANCE) {
					openList.Enqueue(new Vector2(curX, curY - 1));
				}
			}
			// Adding the left neighbor
			if (curX > 0 && boardArray[curX - 1, curY] != TileType.Water) {
				if (Random.value < WATER_EXPANSION_CHANCE) {
					openList.Enqueue(new Vector2(curX - 1, curY));
				}
			}
			// Adding the top neighbor
			if (curY < SIZE - 1 && boardArray[curX, curY + 1] != TileType.Water) {
				if (Random.value < WATER_EXPANSION_CHANCE) {
					openList.Enqueue(new Vector2(curX, curY + 1));
				}
			}
			// Adding the right neighbor
			if (curX < SIZE - 1 && boardArray[curX + 1, curY] != TileType.Water) {
				if (Random.value < WATER_EXPANSION_CHANCE) {
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

				GameObject instance = Instantiate(
					toInstantiate, new Vector3(x * CELL_SIZE, y * CELL_SIZE, 0f), Quaternion.identity) as GameObject;
				instance.transform.SetParent(boardHolder);
			}
		}
	}

	// To be called by the game manager, randomly makes the board
	// and instantiates the tiles in their place
	public void createScene(bool useSeed, int seed) {
		if (useSeed) Random.InitState(seed);

		setupBoard();
		createBoard();
	}
}
