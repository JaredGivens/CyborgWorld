using Godot;
using System;
public enum PrioEnum {
  Idle,
  Stalk,
  Hunt,
  Water,
  Sleep,
  Sex,
};
public class Condition {
  public PrioEnum Prio = PrioEnum.Idle;
  public int Age = 5;
  public int Food = 10;
  public int Water = 10;
  public int Sex = 10;
  public int Energy = 10;
  public int Aura = 0;
  public int Fear = 0;
  public int Stealth = 0;
  public int Perception = 0;
};
public class Destination {
  public Vector3 Position;
  public int Fear;
  public int Distance;
  public int Preference;
}
