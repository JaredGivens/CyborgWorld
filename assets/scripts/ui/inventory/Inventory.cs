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
  private Hotbar _hotbar;
  public void Populate(Player.Save save) {
    _sandbox.Visible = Glob.Save.Gamemode == GamemodeEnum.Sandbox;
    _sandbox.Sandbox();
    _items.BindStacks(save.InventoryStacks);
    //_phases.BindStacks(save.Phases);
  }
}
