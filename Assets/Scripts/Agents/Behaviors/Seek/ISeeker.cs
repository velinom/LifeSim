using UnityEngine;

public interface ISeeker {

  // Seek the given location my moving toward it with a set max-acceleration
  // returns the steering vector to seek the given location
  Vector2 seek(Vector2 location);
}
