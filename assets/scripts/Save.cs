using Godot;
using System;
namespace World {
  public partial class Save : Resource {
    [Export]
    public string Name;
    [Export]
    public int Seed;
    [Export]
    public GamemodeEnum Gamemode;
    [Export]
    public Vector3 Spawn = new(0, 32, 0);
  }
}
