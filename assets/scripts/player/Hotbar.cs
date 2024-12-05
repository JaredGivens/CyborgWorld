using Godot;
using System;
using System.Collections.Generic;

public partial class Hotbar : HBoxContainer {
  public Int32 Selected = 0;
  public UnitSlotTexture[] Slots = new UnitSlotTexture[4];
  private UnitStack[] _binding;
  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    Slots[0] = GetNode<UnitSlotTexture>("VBoxContainer0/UnitSlotTexture0");
    Slots[1] = GetNode<UnitSlotTexture>("VBoxContainer1/UnitSlotTexture1");
    Slots[2] = GetNode<UnitSlotTexture>("VBoxContainer2/UnitSlotTexture2");
    Slots[3] = GetNode<UnitSlotTexture>("VBoxContainer3/UnitSlotTexture3");
  }
  public void BindStacks(UnitStack[] stacks) {
    _binding = stacks;
    for (int i = 0; i < 4; ++i) {
      //Slots[i].Unit.Stack = stacks[i];
    }
  }
  public void UpdateStacks() {
    for (int i = 0; i < 4; ++i) {
      //_binding[i] = Slots[i].Unit.Stack;
    }
  }
}
