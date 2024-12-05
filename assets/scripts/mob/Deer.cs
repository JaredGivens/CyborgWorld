using Godot;
using System;
public partial class Deer : CharacterBody3D {
  public Condition Condition;
  [Export]
  public float Speed = 1.0f;
  public Mob Mob;

  public override void _Ready() {
    Mob = new Mob(this);
  }
  public override void _PhysicsProcess(double delta) {
    Mob.Process(Speed);
  }
}
