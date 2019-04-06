using Models;
using UnityEngine;

public abstract class BaseAgent : MonoBehaviour {

  // Assess the smell of the given type in the 3x3 area surounding the agent,
  // Determine the direction that the smell is coming from and return a 2D 
  // vector in that direction
  public Vector2 getDirectionOfSmell(SmellType type, Vector2 currentCell, Smell[, ] smells) {
    Vector2 smellDirection = new Vector2(0, 0);

    // Loop over all the cells adjacent to the sheep and determine
    // the direction that the smell is strongest
    for (int xOffset = -1; xOffset < 2; xOffset++) {
      for (int yOffset = -1; yOffset < 2; yOffset++) {
        int curX = (int)currentCell.x + xOffset;
        int curY = (int)currentCell.y + yOffset;
        if (curX >= 0 && curX < GameManager.SIZE && curY >= 0 && curY < GameManager.SIZE) {
          double curSmellVal = smells[curX, curY].getSmell(type);
          Vector2 curSmellCell = new Vector2(curX, curY);
          Vector2 curDirection = curSmellCell - currentCell;
          smellDirection = smellDirection + (curDirection * (float)curSmellVal);
        }
      }
    }
    
    smellDirection.Normalize();
    return smellDirection;
  }

  // Return the location of the first object of the given food type within
  // the given distance of the location. If none is found, return (-1, -1)
  public Vector2 getCloseFood(BoardManager.Food type, int distance,
                               Vector2 currentCell, BoardManager.Food[, ] foodArray) {
    for (int xOffset = -distance; xOffset < distance + 1; xOffset++) {
      for (int yOffset = -distance; yOffset < distance + 1; yOffset++) {
        int curX = (int)currentCell.x + xOffset;
        int curY = (int)currentCell.y + yOffset;
        if (curX >= 0 && curX < GameManager.SIZE && curY >= 0 && curY < GameManager.SIZE) {
          if (foodArray[curX, curY] == type) {
            return new Vector2(curX, curY);
          }
        }
      }
    }

    return new Vector2(-1, -1);
  }

  // Return the location of the first object of the given tile type within the given 
  // distance of the location
  public Vector2 getCloseTile(BoardManager.TileType type, int distance,
                               Vector2 currentCell, BoardManager.TileType[, ] boardArray) {
    for (int xOffset = -distance; xOffset < distance + 1; xOffset++) {
      for (int yOffset = -distance; yOffset < distance + 1; yOffset++) {
        int curX = (int)currentCell.x + xOffset;
        int curY = (int)currentCell.y + yOffset;
        if (curX >= 0 && curX < GameManager.SIZE && curY >= 0 && curY < GameManager.SIZE) {
          if (boardArray[curX, curY] == type) {
            return new Vector2(curX, curY);
          }
        }
      }
    }

    // If we didn't find any tiles of the given type, return "dummy" vector (-1, -1)
    return new Vector2(-1, -1);
  }

  // Calculates the force needed to make the agent slowly arrive at the
  // given location and gradualy come to a stop
  public Vector2 arriveAt(Vector2 targetLoc, Vector2 currentLoc, Vector2 currentVel, 
                          float slowRad, float targetRad, float maxSpeed) {
    float distToTarget = (targetLoc - currentLoc).magnitude;

    // Three distance cases from slow-radius and target-radius
    // Compute the target velocity in each case
    float targetSpeed;
    if (distToTarget > slowRad) {
      targetSpeed = maxSpeed;
    } else if (distToTarget > targetRad) {
      targetSpeed = (distToTarget / slowRad) * maxSpeed;
    } else {
      targetSpeed = 0;
    }

    // Get the target velocity including direction
    Vector2 targetVelocity = (targetLoc - currentLoc);
    targetVelocity.Normalize();
    targetVelocity = targetVelocity * targetSpeed;

    // The target acceleration is the difference between the current velocity
    // and the target velocity
    return targetVelocity - currentVel;
  }
}