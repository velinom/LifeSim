using System;

// Using Serializable allows us to embed a class with sub properties in the inspector.
[Serializable]
// Class to easilly specify a min and max number of something. Useful for precedural
// generation of some number of objects in a range.
public class Count {
  public int minimum;             //Minimum value for our Count class.
  public int maximum;             //Maximum value for our Count class.
            
  //Assignment constructor.
  public Count (int min, int max) {
    minimum = min;
    maximum = max;
  }
}