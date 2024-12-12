using Godot;
using System;
using System.Runtime.InteropServices;

[Flags]
public enum UnitType {
  None = 0,
  Item = 1,
  Terraform = 2,
  Phase = 4,
  Spell = 8,
}
[StructLayout(LayoutKind.Explicit)]
public struct UnitData {
}
public partial class Unit : Resource {
  [Export]
  public String Name;
  [Export(PropertyHint.MultilineText)]
  public String Desc;
  [Export]
  public Texture2D Icon;
  [Export]
  public UnitType Type;
  //public UnitData data;
  [Export]
  public AoeShape Shape;
  [Export]
  public Basis Basis;
}
