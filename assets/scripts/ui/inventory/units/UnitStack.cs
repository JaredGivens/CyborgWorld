using Godot;
using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit)]
public struct UnitStack {
  [FieldOffset(0)] private Int16 _int;
  [FieldOffset(0)] public Byte Id;
  [FieldOffset(1)] public Byte Amt;
  public UnitStack(Int32 id = 0, Int32 amt = 0) {
    _int = 0;
    Id = (Byte)id;
    Amt = (Byte)amt;
  }
  public static explicit operator UnitStack(Variant v) {
    if (v.VariantType == Variant.Type.Int) {
      return new UnitStack { _int = v.AsInt16() };
    } else {
      GD.PrintErr("cast fail");
      return new UnitStack();
    }
  }
  public static implicit operator Int16(UnitStack s) => s._int;
  public static explicit operator UnitStack(Int16 i) => new UnitStack { _int = i };
};
