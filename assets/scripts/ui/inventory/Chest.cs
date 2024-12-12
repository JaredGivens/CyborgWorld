using Godot;
using System;
public partial class Chest : BlockControl {
  [Export]
  private UnitGrid _items;
  public override void BindStacks(Memory<Int16> stacks) {
    _items.BindStacks(stacks);
  }
}
