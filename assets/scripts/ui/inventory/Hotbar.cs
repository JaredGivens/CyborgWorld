using Godot;
using System;
using System.Collections.Generic;

public partial class Hotbar : HBoxContainer {
  public Int32 Selected = 0;
  public UnitTexture[] Units = new UnitTexture[4];
  [Export]
  public Boolean Mutable = false;
  private Int16[] _binding;
  // Called when the node enters the scene tree for the first time.

  public override void _Ready() {
    for (Int32 i = 0; i < 4; ++i) {
      Units[i] = GetNode<UnitTexture>($"VBoxContainer{i}/UnitSlotTexture{i}/UnitTexture");
    }
  }
  public void BindStacks(Int16[] stacks) {
    _binding = stacks;
    for (Int32 i = 0; i < 4; ++i) {
      var icopy = i;
      Units[i].Init((UnitStack)stacks[i], Mutable,
       UnitType.Item | UnitType.Terraform | UnitType.Spell, () => {
         _binding[icopy] = (Int16)Units[icopy].Stack;
       });
    }
  }
  public void UpdateStacks() {
    for (Int32 i = 0; i < 4; ++i) {
      Units[i].Stack = (UnitStack)_binding[i];
    }
  }
}
