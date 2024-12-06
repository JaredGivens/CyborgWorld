using Godot;
using System;
using System.Collections.Generic;
public partial class Inventory : ColorRect {
  // Called when the node enters the scene tree for the first time.
  private UnitGrid _items;
  private UnitGrid _phases;
  private UnitGrid _sandbox;
  private Hotbar _hotbar;
  public override void _Ready() {
    _hotbar = GetNode<Hotbar>("CenterContainer/VBoxContainer/Hotbar");
    _phases = GetNode<UnitGrid>
      ("CenterContainer/VBoxContainer/HBoxContainer/PhaseSlots");
    _items = GetNode<UnitGrid>
      ("CenterContainer/VBoxContainer/HBoxContainer/ItemSlots");
    _sandbox = GetNode<UnitGrid>
      ("CenterContainer/VBoxContainer/HBoxContainer/SandboxSlots");
  }
  public void Populate(Player.Save save) {
    _sandbox.Visible = Glob.Save.Gamemode == GamemodeEnum.Sandbox;
    _sandbox.Sandbox();
    _items.BindStacks(save.InventoryStacks);
    //_phases.BindStacks(save.Phases);
    _hotbar.BindStacks(save.HotbarStacks);
  }
}
