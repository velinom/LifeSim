using Models;
using UnityEngine;

public class SmellFollower : ISmellFollower {

  // Implemented form the interface, get the direction that the smell
  // is increasing the most as a unit vector in 2D space.
  public Vector2 directionOfSmell(Vector2 location, Smell[, ] smells, SmellType type) {
    // Loop over all the cells adjacent to the smell follower and determine
    // the direction that the smell is strongest
    Vector2 smellDirection = new Vector2(0, 0);
    for (int xOffset = -1; xOffset < 2; xOffset++) {
      for (int yOffset = -1; yOffset < 2; yOffset++) {
        int curX = (int)location.x + xOffset;
        int curY = (int)location.y + yOffset;
        if (curX >= 0 && curX < smells.GetLength(0) && curY >= 0 && curY < smells.GetLength(0)) {
          double curSmellVal = smells[curX, curY].getSmell(type);
          Vector2 curSmellCell = new Vector2(curX, curY);
          Vector2 curDirection = curSmellCell - location;
          smellDirection = smellDirection + (curDirection * (float)curSmellVal);
        }
      }
    }
    
    smellDirection.Normalize();
    return smellDirection;
  }
}