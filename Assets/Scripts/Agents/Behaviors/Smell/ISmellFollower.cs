using Models;
using UnityEngine;

public interface ISmellFollower {

  // Get the direction of a smell in the world
  Vector2 directionOfSmell(Vector2 location, Smell[, ] smells, SmellType type);
}