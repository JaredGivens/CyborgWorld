using Godot;
using System;

[Flags]
public enum UnitType {
  None = 0,
  Item = 1,
  Block = 2,
  Phase = 4,
  Spell = 8,
}
public partial class Unit : Resource {
  [Export]
  public string Name;
  [Export(PropertyHint.MultilineText)]
  public string Desc;
  [Export]
  public Texture2D Icon;
  [Export]
  public UnitType Type;
}
