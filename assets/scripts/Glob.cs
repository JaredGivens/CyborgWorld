using System;
using Godot;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

static class Glob {
  public static RenderingDevice RD;
  public const sbyte DistFac = 8;
  public const sbyte SdfRange = 128 / DistFac;
  public static World.Save Save;
  public static List<Unit> Units;
  public static string SavePath;
  public static Vector3I Unflat(Int32 f, Int32 dimLen) {
    return new Vector3I(f / dimLen / dimLen,
        f / dimLen % dimLen,
        f % dimLen);
  }
  public static int Flat(Vector3I v, Int32 dimLen) {
    return v.X * dimLen * dimLen + v.Y * dimLen + v.Z;
  }
  public static int Flat(Vector3I v, Vector3I dims) {
    return v.X * dims.Y * dims.Z + v.Y * dims.Z + v.Z;
  }
  public static IEnumerable<(Int32, Int32)> Inds<T>(T[,] mat) {
    for (Int32 x = 0; x < mat.GetLength(0); ++x) {
      for (Int32 y = 0; y < mat.GetLength(1); ++y) {
        yield return (x, y);
      }
    }
  }
  public static void Resize<T>(this List<T> list, Int32 sz, T c) {
    Int32 cur = list.Count;
    if (sz < cur)
      list.RemoveRange(sz, cur - sz);
    else if (sz > cur) {
      if (sz > list.Capacity) //this bit is purely an optimisation, to avoid multiple automatic capacity changes.
        list.Capacity = sz;
      list.AddRange(Enumerable.Repeat(c, sz - cur));
    }
  }
  public static void Resize<T>(this List<T> list, Int32 sz) where T : new() {
    Resize(list, sz, new T());
  }
  public static Int32 DivFloor(Single v, Single d) {
    return (Int32)MathF.Floor(v / d);
  }
  public static Vector3I DivFloor(Vector3 v, Single d) {
    return (Vector3I)(v / d).Floor();
  }
  public static Int32 DivFloor(Int32 n, Int32 d) {
    return n - Mod(n, d);
  }
  public static Int32 DivFloor2(Int32 n, Int32 d) {
    return n - Mod2(n, d);
  }
  public static Vector3I DivFloor(Vector3I v, Int32 d) {
    return v - Mod(v, d);
  }
  public static Vector3I DivFloor2(Vector3I v, Int32 d) {
    return v - Mod2(v, d);
  }
  public static Vector3I DivFloor(Vector3I v, Vector3I d) {
    return v - Mod(v, d);
  }
  public static Vector3I DivFloor2(Vector3I v, Vector3I d) {
    return v - Mod2(v, d);
  }
  public static Vector3I Mod(Vector3I v, Vector3I m) {
    var r = v % m;
    r.X = r.X < 0 ? r.X + m.X : r.X;
    r.Y = r.Y < 0 ? r.Y + m.Y : r.Y;
    r.Z = r.Z < 0 ? r.Z + m.Z : r.Z;
    return r;
  }
  public static Vector3I Mod2(Vector3I v, Vector3I m) {
    v.X &= m.X - 1;
    v.Y &= m.Y - 1;
    v.Z &= m.Z - 1;
    return v;
  }
  public static Vector3I Mod(Vector3I v, Int32 m) {
    var r = v % m;
    r.X = r.X < 0 ? r.X + m : r.X;
    r.Y = r.Y < 0 ? r.Y + m : r.Y;
    r.Z = r.Z < 0 ? r.Z + m : r.Z;
    return r;
  }
  public static Vector3I Mod2(Vector3I v, Int32 m) {
    v.X &= m - 1;
    v.Y &= m - 1;
    v.Z &= m - 1;
    return v;
  }
  public static int ModFlat(Vector3I v, Int32 m) {
    return Flat(Mod(v, m), m);
  }
  public static int ModFlat2(Vector3I v, Int32 m) {
    return Flat(Mod2(v, m), m);
  }
  public static int Mod2(Int32 x, Int32 m) {
    return x & (m - 1);
  }
  public static int Mod(Int32 x, Int32 m) {
    Int32 r = x % m;
    return r < 0 ? r + m : r;
  }
}
