using Godot;
using System;
using System.Collections.Generic;
public partial class Inventory : ColorRect {
  // Called when the node enters the scene tree for the first time.
  [Export]
  private UnitGrid _items;
  [Export]
  private UnitGrid _phases;
  [Export]
  private UnitGrid _sandbox;
  [Export]
  private HBoxContainer _grids;
  private Hotbar _hotbar;
  private Control Dependency;
  public void Populate(Player.Save save, Control? nextDependency = null) {
    _sandbox.Visible = Glob.Save.Gamemode == GamemodeEnum.Sandbox;
    _sandbox.Sandbox();
    _items.BindStacks(save.InventoryStacks);
    if (Dependency is Control prev) {
      _grids.RemoveChild(prev);
    }
    Dependency = nextDependency;
    if (nextDependency is Control next) {
      _grids.AddChild(next);
      _grids.MoveChild(next, 1);
    }
    //_phases.BindStacks(save.Phases);
  }
}
