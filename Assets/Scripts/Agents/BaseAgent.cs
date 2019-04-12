using Models;
using System.Collections.Generic;
using UnityEngine;

// Class with a bunch of utility methods that are the same / used acrossed several
// differnet agents
public abstract class BaseAgent : MonoBehaviour {

  // Consts from the game manager
  public float CELL_SIZE = GameManager.CELL_SIZE;

  // MOVEMENT CONSTANTS,
  // THESE NEED TO BE SET IN IMPLEMENTING CLASSES
  public float MAX_SPEED;
  public float MAX_ACCEL;
  public float MAX_ROTATION;
  public float MAX_ANGULAR_ACC;
  // The radii for the arrive-at behavior
  public float ARRIVE_RADIUS;
  public float SLOW_RADIUS;
  public float ROTATE_ARRIVE_RAD;
  public float ROTATE_SLOW_RAD;

  // The velocity and rotation of the agent.
  public Vector2 velocity;
  public float rotation;

  // The cell that the agent is currently in
  public Vector2 currentCell;

  // The action that the agent is currently taking
  // Can breifly be null while the agent "Thinks" about
  // what to do
  public Action goal;

  // The insistance object for this agent, the agent's goal is to minimize 
  // the values in this object.
  public Insistance insistance;

  // The location this agent is currently attempting to arrive at. This only
  // happens once the agent is very close to and "sees" their goal.
  // Is frequently null if the agent is wandering, sleeping, or following a smell
  public Vector2 target;

  // Uses the transfrom of this GameObject to determine what cell the sheep is 
  // currently in.
  public Vector2 getCurrentCell() {
    int xCell = (int)Mathf.Round(this.transform.position.x / CELL_SIZE);
    int yCell = (int)Mathf.Round(this.transform.position.y / CELL_SIZE);

    return new Vector2(xCell, yCell);
  }

  // Picks the best goal in the list of actions to minimize the sum of the insistances
  // squared. Accounts for the time that actions take
  public void determineGoal(List<Action> actions, Insistance insistance, string name) {
    // Loop over all available actions and determine the one that minimizes the 
    // sum of insistances sqared. Use the estimated time to complete each action
    // to discount the other actions when calculating
    float bestValue = float.MaxValue;
    Action bestAction = null;
    foreach (Action action in actions) {
      // Begin by cloning the insistance object so it isn't mutated in the calculations
      Insistance insistanceCopy = insistance.deepCopy();

      // This method also uses the estimated time to determine how much the insistances
      // will increase while this action is being carried out.
      action.takeActionAtTime(insistanceCopy, action.estTimeSeconds);
      float totalInsistance = insistanceCopy.totalInsistance(); // sum of squares
      if (totalInsistance < bestValue) {
        bestValue = totalInsistance;
        bestAction = action;
      }
    }

    Debug.Log("A " + name + " with: hunger:" + insistance.insistances[InsistanceType.Food] +
              ", thirst:" + insistance.insistances[InsistanceType.Water] +
              ", sleep:" + insistance.insistances[InsistanceType.Sleep] +
              ", joy:" + insistance.insistances[InsistanceType.Joy] +
              ", Decided to " + bestAction.name);

    this.goal = bestAction;
  }

  // ACTION METHOD: Returns the main steering vector to accomplish this action, 
  // allong with setting this.action to null and updating insistances when the 
  // action is finished
  public Vector2 seekFood(BoardManager.Food foodType, SmellType smellType) {
    Vector2 goalSteering = new Vector2(-1, -1);

    // If we've seen the food and stored its location, just arrive at the location
    // (If we don't have a target to arrive at the vector is (-1, -1))
    if (this.target.x >= 0 && this.target.y >= 0) {
      goalSteering = arriveAt(
        new Vector2(this.target.x * CELL_SIZE, this.target.y * CELL_SIZE),
        new Vector2(currentCell.x * CELL_SIZE, currentCell.y * CELL_SIZE),
        velocity, SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
      
      // If we have arrived at the location, apply the current goal to the insistances
      if (this.target.x == this.currentCell.x && this.target.y == this.currentCell.y) {
        this.goal.apply(this.insistance);
        this.goal = null;
        this.target = new Vector2(-1, -1);
      }
    } else {
      // If we haven't seen any food yet, check if we see one
      // Check if within 3 blocks of food type (represents the agent seeing the food)
      Vector2 closeBush = getCloseFood(foodType, 3, currentCell, GameManager.instance.getFoodArray());
      if (closeBush.x >= 0 && closeBush.y >= 0) {
        this.target = closeBush;
        goalSteering = arriveAt(
          new Vector2(closeBush.x * CELL_SIZE, closeBush.y * CELL_SIZE),
          new Vector2(currentCell.x * CELL_SIZE, currentCell.y * CELL_SIZE),
          velocity, SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
      } else {
        // If we still can't see a bush, just follow the smell
        goalSteering = this.getDirectionOfSmell(smellType, currentCell,
          GameManager.instance.getSmellArray()) * MAX_ACCEL;
      }
    }

    return goalSteering;
  }

  // ACTION METHOD: Returns the main steering vector to accomplish this action, 
  // allong with setting this.action to null and updating insistances when the 
  // action is finished
  public Vector2 seekTile(BoardManager.TileType tileType, SmellType smellType) {
    Vector2 goalSteering = new Vector2(-1, -1);
    // If we've seen the tile and stored its location, just arrive at the tile
    // (If we don't have a target to arrive at the vector is (-1, -1))
    if (this.target.x >= 0 && this.target.y >= 0) {
      goalSteering = arriveAt(
        new Vector2(this.target.x * CELL_SIZE, this.target.y * CELL_SIZE),
        new Vector2(currentCell.x * CELL_SIZE, currentCell.y * CELL_SIZE),
        velocity, SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
      
      // If we have arrived at the target, the agent should execute the action
      BoardManager.TileType currentCellType =
        GameManager.instance.getBoardArray()[(int)currentCell.x, (int)currentCell.y];
      if (this.target.x == this.currentCell.x && this.target.y == this.currentCell.y ||
          currentCellType == BoardManager.TileType.Water) {
        this.goal.apply(this.insistance);
        this.goal = null;
        this.target = new Vector2(-1, -1);
      }
    } else {
      // If we haven't seen tile yet, check if we see one
      // Check if within 3 blocks of the tile (represents the agent seeing the tile and going)
      Vector2 closeTile = getCloseTile(tileType, 3, currentCell, GameManager.instance.getBoardArray());
      if (closeTile.x >= 0 && closeTile.y >= 0) {
        this.target = closeTile;
        goalSteering = arriveAt(
          new Vector2(closeTile.x * CELL_SIZE, closeTile.y * CELL_SIZE),
          new Vector2(currentCell.x * CELL_SIZE, currentCell.y * CELL_SIZE),
          velocity, SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
      } else {
        // If we still can't see a bush, just follow the smell
        goalSteering = this.getDirectionOfSmell(SmellType.Water,
          currentCell, GameManager.instance.getSmellArray()) * MAX_ACCEL;
      }
    }

    return goalSteering;
  }

  // ACTION METHOD: Sleep the agent for some given time, make sure that it is not 
  // moving.
  private float sleepStartTime = -1;
  public Vector2 sleep(ParticleSystem sleepParticles, float timeToSleep) {
    // Start sleeping
    if (sleepStartTime < 0) {
      sleepStartTime = Time.time;
      sleepParticles.Play();
    }

    // If we have been sleeping for given time (in seconds)
    if (Time.time > sleepStartTime + timeToSleep) {
      this.goal.apply(this.insistance);
      this.goal = null;
      this.target = new Vector2(-1, -1);
      this.sleepStartTime = -1;
      sleepParticles.Stop();
    }

    // Make sure the agent isn't moving
    this.rotation = 0;
    return new Vector2(-this.velocity.x, -this.velocity.y);
  }

  // ACTION METHOD make the agent wander using a seek behavior toward a point.
  // The point that the agent is seeking is based on the agnet's current velocity
  // and is offset by some random ammount
  private float wanderStartTime = -1;
  private float wanderAngle; // The angle of the target on the circle
  public Vector2 wander() {
    // First time wander is called, setup the circle where the seek location
    // will live
    if (this.wanderStartTime < 0) {
      wanderStartTime = Time.time;
      
      // Pick an angle between 0 and 360
      wanderAngle = Random.Range(0, 360);
    }

    // Update the wnader angle and Calculate the point to seek
    // The point to seek is on a circle in front of the agent at the wander angle
    // the wander angle moves randomly every frame.
    wanderAngle += Random.Range(-15, 15);
    Vector2 inFrontOfSheep = this.transform.position + this.transform.right * 2;
    Vector2 fromCenterToEdge = new Vector2(Mathf.Cos(wanderAngle), Mathf.Sin(wanderAngle));
    Vector2 pointOnCircle = inFrontOfSheep + fromCenterToEdge;

    if (Time.time > this.wanderStartTime + 10) {
      this.goal.apply(this.insistance);
      this.goal = null;
      this.target = new Vector2(-1, -1);
      this.wanderStartTime = -1;
    }

    return arriveAt(pointOnCircle, this.transform.position, this.velocity,
                    SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
  }

  // Return a steering vector to awoid any walls. Need to avoid High elevation and water.
  // Done by using short "whisker" ray-casts and moving to a spot normal to the wall
  // if the whiskers hit something. Use one long ray forward, and two shorter side rays.
  private float MAIN_RAY_LENGTH = 2;
  private float SIDE_RAY_LENGTH = 0.8f;
  public Vector2 calculateWallAvoidance() {
    // If we have a goal and it's seeking water, don't wall-avoid water
    bool shouldAvoidWater = true;
    if (this.goal != null) {
      shouldAvoidWater = this.goal.name != "Seek Water";
    }

    // Preform the main whisker ray-cast
    RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.right, MAIN_RAY_LENGTH);
    if (hit.collider != null) {
      if ((hit.transform.tag == "Water" && shouldAvoidWater) || 
           hit.transform.tag == "HighElevation") {
        // Get a point normal to the wall at the point the colider hit.
        Vector2 normal = hit.normal.normalized;
        Vector2 seekPoint = hit.point + hit.normal * 1.8f;
        Vector2 curLoc = new Vector2(currentCell.x * CELL_SIZE, currentCell.y * CELL_SIZE);
        return arriveAt(seekPoint, curLoc, this.velocity, SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
      }
    }
    // Preform the side whisker ray-casts
    Vector3 leftDirection = Quaternion.AngleAxis(-40, transform.forward) * transform.right;
    RaycastHit2D lSideHit = Physics2D.Raycast(transform.position, leftDirection, SIDE_RAY_LENGTH);
    if (lSideHit.collider != null) {
      if ((lSideHit.transform.tag == "Water" && shouldAvoidWater) || 
           lSideHit.transform.tag == "HighElevation") {
        // Get a point normal to the wall at the point the colider hit.
        Vector2 normal = lSideHit.normal.normalized;
        Vector2 seekPoint = lSideHit.point + normal * 1.2f;
        Vector2 curLoc = new Vector2(currentCell.x * CELL_SIZE, currentCell.y * CELL_SIZE);
        return arriveAt(seekPoint, curLoc, this.velocity, SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
      }
    }
    Vector3 rightDirection = Quaternion.AngleAxis(40, transform.forward) * transform.right;
    RaycastHit2D rSideHit = Physics2D.Raycast(
      this.transform.position, rightDirection, SIDE_RAY_LENGTH);
    if (rSideHit.collider != null) {
      if ((rSideHit.transform.tag == "Water" && shouldAvoidWater) || 
           rSideHit.transform.tag == "HighElevation") {
        // Get a point normal to the wall at the point the colider hit.
        Vector2 normal = rSideHit.normal.normalized;
        Vector2 seekPoint = rSideHit.point + normal * 0.8f;
        Vector2 curLoc = new Vector2(currentCell.x * CELL_SIZE, currentCell.y * CELL_SIZE);
        return arriveAt(seekPoint, curLoc, this.velocity, SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
      }
    }

    return new Vector2(0, 0);
  }

  // Gets all the sheep and wolves within the fixed radius of the agent.
  // Calculate the point of closest approach for the agent and each sheep or wolf
  // If there will be a collision, get repelled from the point of collision
  private const float COLLISION_AVOIDANCE_RAD = 4;
  public Vector2 calculateCollisionAvoidance() {
    Vector2 steering = new Vector2(0, 0);

    // Avoid sheep
    foreach (BaseAgent sheep in GameManager.instance.getSpawnedSheep()) {
      if (sheep == null) break;

      Vector2 relativePosition = this.transform.position - sheep.transform.position;
      if (relativePosition.magnitude < COLLISION_AVOIDANCE_RAD) {
        // Determine the point of closest approach
        Vector2 relativeVelocity = this.velocity - sheep.velocity;
        float timeToClosest = Vector2.Dot(relativePosition, relativeVelocity) / 
          (relativeVelocity.magnitude * relativeVelocity.magnitude);
        Vector2 projectedLoc = (Vector2)transform.position + (this.velocity * timeToClosest);
        Vector2 sheepProjLoc = (Vector2) sheep.transform.position + (sheep.velocity * timeToClosest);
        Vector2 betweenClosest = projectedLoc - sheepProjLoc;
        if (betweenClosest.magnitude < 0.9) {
          // Scale force with the inverse of the distance
          Vector2 awayFromSheep = (Vector2)transform.position - sheepProjLoc;
          float scale = 1.0f / awayFromSheep.magnitude;
          awayFromSheep.Normalize();
          awayFromSheep *= scale;
          steering += awayFromSheep;
        }
      }
    }
    // Avoid wolves
    foreach (BaseAgent wolf in GameManager.instance.getSpawnedWolves()) {
      Vector2 relativePosition = this.transform.position - wolf.transform.position;
      if (relativePosition.magnitude < COLLISION_AVOIDANCE_RAD) {
        // Determine the point of closest approach
        Vector2 relativeVelocity = this.velocity - wolf.velocity;
        float timeToClosest = Vector2.Dot(relativePosition, relativeVelocity) / 
          (relativeVelocity.magnitude * relativeVelocity.magnitude);
        Vector2 projectedLoc = (Vector2)transform.position + (this.velocity * timeToClosest);
        Vector2 wolfProjLoc = (Vector2) wolf.transform.position + (wolf.velocity * timeToClosest);
        Vector2 betweenClosest = projectedLoc - wolfProjLoc;
        if (betweenClosest.magnitude < 0.9) {
          // Scale force with the inverse of the distance
          Vector2 awayFromWolf = (Vector2)transform.position - wolfProjLoc;
          float scale = 1.0f / awayFromWolf.magnitude;
          awayFromWolf.Normalize();
          awayFromWolf *= scale;
          steering += awayFromWolf;
        }
      }
    }

    return steering;
  }

  // Mutates the given insistance by increasing all insistances based on their
  // growth rates TODO MOVE TO CLASS
  public void increaseInsistances(Insistance insistance) {
    foreach (InsistanceType type in insistance.insistanceTypes) {
      insistance.insistances[type] += insistance.growthRates[type] * Time.deltaTime;
    }
  }

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

  // Calculates the acceleration to seek a given target with a given velocity
  public Vector2 pursue(Vector2 targetLoc, Vector2 targetVel) {
    // Determine the time to reach the target's current location
    Vector2 targetDirection = (Vector2)this.transform.position - targetLoc;
    float timeToTarget = targetDirection.magnitude / this.velocity.magnitude;

    // Project the target forward by that ammount of time
    Vector2 seekLocation = targetLoc + targetVel * timeToTarget;

    // Move at max accel toward the seek location
    return seek(seekLocation);
  }

  // Accelerate at max value in the direction of the target
  public Vector2 seek(Vector2 target) {
    Vector2 direction = target - (Vector2)this.transform.position;

    // Scale the vector in direction to max-acceleration
    direction.Normalize();
    return direction * MAX_ACCEL;
  }

  // Accelerate at max value in the away from the target
  public Vector2 flee(Vector2 target) {
    Vector2 direction = target - (Vector2)this.transform.position;

    // Scale the vector in direction to max-acceleration
    direction.Normalize();
    return direction * -MAX_ACCEL;
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

  // Used for displaying the info about this agent when it is clicked
  void OnMouseDown() {
    HudController.instance.setInfo(this);
  }
}
