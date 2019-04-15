public interface IAligner {

  // Aligns the agent to face in the direction that its rigidboy
  // is moving. Returns the steering vector to do the allignment.
  float align();
}