using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
public partial class UnitGrid : ScrollContainer {
  static PackedScene _unitSlotTexPacked =
      GD.Load<PackedScene>("res://scenes/ui/inventory/units/unit_slot_texture.tscn");
  [Export]
  public UnitType Types = UnitType.Spell | UnitType.Item | UnitType.Block;
  [Export]
  public bool Droppable = false;
  [Export]
  public Int32 Columns = 8;
  [Export]
  public Int32 SlotAmt = 64;
  private List<UnitTexture> _units = new();
  private GridContainer _grid;
  private Int16[] _binding;
  public override void _Ready() {
    _grid = GetNode<GridContainer>("GridContainer");
    _grid.Columns = Columns;
    for (Int32 i = 0; i < SlotAmt; ++i) {
      var slot = _unitSlotTexPacked.Instantiate();
      var unit = slot.GetNode<UnitTexture>("UnitTexture");
      _units.Add(unit);
      _grid.AddChild(slot);
      slot.SetProcess(false);
    }
  }
  public void Sandbox() {
    Types = UnitType.Item | UnitType.Block | UnitType.Phase;
    Droppable = true;
    for (Int32 i = 0; i < Glob.Units.Count; ++i) {
      _units[i].Stack = new UnitStack(i, 1);
    }
  }
  public void UpdateStacks() {
    for (Int32 i = 0; i < SlotAmt; ++i) {
      _units[i].Stack = (UnitStack)_binding[i];
    }
  }
  public void BindStacks(Int16[] stacks) {
    _binding = stacks;
    for (Int32 i = 0; i < SlotAmt; ++i) {
      var icopy = i;
      _units[i].Init((UnitStack)stacks[i], !Droppable, Types, () => {
        _binding[icopy] = _units[icopy].Stack;
      });
    }
  }
  public override bool _CanDropData(Vector2 pos, Variant data) {
    var stack = (UnitStack)data;
    if (stack.Amt == 0) {
      return false;
    }
    var type = Glob.Units[stack.Id].Type;
    return Droppable && (type & Types) != UnitType.None;
  }
  public override void _DropData(Vector2 pos, Variant data) {
    var stack = (UnitStack)data;
    var unit = _units.Find(unit => unit.Stack.Id == stack.Id);
    stack.Amt += 1;
    unit.Stack = stack;
  }
}
