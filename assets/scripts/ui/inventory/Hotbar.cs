using Godot;
using System;
using System.Collections.Generic;

public partial class Hotbar : HBoxContainer {
  private static LabelSettings _defaultSettings =
    GD.Load<LabelSettings>("res://resources/HudLabel.tres");
  private static LabelSettings _selectedSettings =
    GD.Load<LabelSettings>("res://resources/HudLabelSelected.tres");
  private Int32 _selected = 0;
  public Int32 Selected {
    get => _selected;
    set {
      Labels[_selected].LabelSettings = _defaultSettings;
      Labels[value].LabelSettings = _selectedSettings;
      _selected = value;
    }
  }
  public UnitTexture[] Units = new UnitTexture[4];
  public Label[] Labels = new Label[4];
  [Export]
  public Boolean Mutable = false;
  private Int16[] _binding;
  // Called when the node enters the scene tree for the first time.

  public override void _Ready() {
    for (Int32 i = 0; i < 4; ++i) {
      Labels[i] = GetNode<Label>($"VBoxContainer{i}/Label");
      Units[i] = GetNode<UnitTexture>($"VBoxContainer{i}/UnitSlotTexture{i}/UnitTexture");
    }
    Labels[Selected].LabelSettings = _selectedSettings;
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
