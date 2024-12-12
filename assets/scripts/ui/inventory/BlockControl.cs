using System;
using Godot;
public abstract partial class BlockControl : Control {
  public abstract void BindStacks(Memory<Int16> stacks);
}
