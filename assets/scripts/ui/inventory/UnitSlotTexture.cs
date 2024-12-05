using Godot;
using System;
public partial class UnitSlotTexture : TextureRect {
  [Export]
  public bool Mutable = true;
  [Export]
  public UnitType Types = UnitType.Spell | UnitType.Item | UnitType.Block;
  public UnitTexture Unit;
  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    Unit = GetNode<UnitTexture>("UnitTexture");
    Unit.Mutable = Mutable;
    Unit.Types = Types;
  }
}
