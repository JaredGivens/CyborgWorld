using Godot;
using System;
public enum PhaseType {
  Projectile,
  Area,
  Modification,
  Extension,
  Condition,
}
public partial class Phase : Resource {
  [Export]
  public float Speed;
  [Export]
  public PhaseType Type;
  [Export]
  public AoeShape AoeShape;
  public Aoe Aoe;
}
