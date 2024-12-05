using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MobSpawner {
  private PackedScene _deer = (PackedScene)ResourceLoader.Load("res://mob/deer.tscn");
  private List<Mob> _mobs = new();
  private Node3D _root;
  [Export]
  private int _mobCap = 32;
  public MobSpawner(Node3D root) {
    _root = root;
  }
  public void Spawn(Vector3 pos) {
  }
}
