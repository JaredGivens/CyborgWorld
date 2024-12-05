using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
public partial class UnitSlots : ScrollContainer {
  [Export]
  public UnitType Types = UnitType.Spell | UnitType.Item | UnitType.Block;
  [Export]
  public bool Mutable = false;
  [Export]
  public Int32 Columns = 8;
  [Export]
  public Int32 SlotAmt = 64;
  private List<UnitSlotTexture> _slots = new();
  private GridContainer _grid;
  private UnitStack[] _binding;
  static PackedScene _unitSlotTexPacked =
      GD.Load<PackedScene>("res://scenes/ui/unit_slot_texture.tscn");
  public override void _Ready() {
    _grid = GetNode<GridContainer>("GridContainer");
    _grid.Columns = Columns;
    for (Int32 i = 0; i < SlotAmt; ++i) {
      //var slot = _unitSlotTexPacked.Instantiate<UnitSlotTexture>();
      //slot.Types = Types;
      //slot.Mutable = !Mutable;
      //_grid.AddChild(slot);
      //_slots.Add(slot);
    }
  }
  public void Sandbox() {
    Types = UnitType.Item | UnitType.Block | UnitType.Phase;
    Mutable = false;
    for (Int32 i = 0; i < Glob.Units.Count; ++i) {
      _slots[i].Unit.Stack = new UnitStack(i, 1);
    }
  }
  public void UpdateStacks() {
    for (Int32 i = 0; i < SlotAmt; ++i) {
      _binding[i] = _slots[i].Unit.Stack;
    }
  }
  public void BindStacks(UnitStack[] Stacks) {
    _binding = Stacks;
    for (Int32 i = 0; i < SlotAmt; ++i) {
      _slots[i].Unit.Stack = Stacks[i];
    }
  }
  public void SetOnDrop(Action action) {
    //foreach (var slot in _slots) {
    //slot.Unit.OnDrop = action;
    //}
  }
  public override bool _CanDropData(Vector2 pos, Variant data) {
    var stack = (UnitStack)data;
    if (stack.Amt == 0) {
      return false;
    }
    var type = Glob.Units[stack.Id].Type;
    return Mutable && (type & Types) != UnitType.None;
  }
  public override void _DropData(Vector2 pos, Variant data) {
    var stack = (UnitStack)data;
    var slot = _slots.Find(slot => slot.Unit.Stack.Id == stack.Id);
    stack.Amt += 1;
    slot.Unit.Stack = stack;
  }
}
