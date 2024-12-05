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
<<<<<<< HEAD
  public static Vector3I Unflat(Int32 f, Int32 dimLen) {
=======
  public static IEnumerable<(int, int, int)> Inds<T>(T[,,] mat) {
    for (int x = 0; x < mat.GetLength(0); ++x) {
      for (int y = 0; y < mat.GetLength(1); ++y) {
        for (int z = 0; z < mat.GetLength(2); ++z) {
          yield return (x, y, z);
        }
      }
    }
  }
  public static Vector3I Unflat(int f, int dimLen) {
>>>>>>> 82d442f (first commit:)
    return new Vector3I(f / dimLen / dimLen,
        f / dimLen % dimLen,
        f % dimLen);
  }
<<<<<<< HEAD
  public static int Flat(Vector3I v, Int32 dimLen) {
=======
  public static int Flat(Vector3I v, int dimLen) {
>>>>>>> 82d442f (first commit:)
    return v.X * dimLen * dimLen + v.Y * dimLen + v.Z;
  }
  public static int Flat(Vector3I v, Vector3I dims) {
    return v.X * dims.Y * dims.Z + v.Y * dims.Z + v.Z;
  }
<<<<<<< HEAD
  public static IEnumerable<(Int32, Int32)> Inds<T>(T[,] mat) {
    for (Int32 x = 0; x < mat.GetLength(0); ++x) {
      for (Int32 y = 0; y < mat.GetLength(1); ++y) {
=======
  public static IEnumerable<(int, int)> Inds<T>(T[,] mat) {
    for (int x = 0; x < mat.GetLength(0); ++x) {
      for (int y = 0; y < mat.GetLength(1); ++y) {
>>>>>>> 82d442f (first commit:)
        yield return (x, y);
      }
    }
  }
<<<<<<< HEAD
  public static void Resize<T>(this List<T> list, Int32 sz, T c) {
    Int32 cur = list.Count;
=======
  public static void Resize<T>(this List<T> list, int sz, T c) {
    int cur = list.Count;
>>>>>>> 82d442f (first commit:)
    if (sz < cur)
      list.RemoveRange(sz, cur - sz);
    else if (sz > cur) {
      if (sz > list.Capacity) //this bit is purely an optimisation, to avoid multiple automatic capacity changes.
        list.Capacity = sz;
      list.AddRange(Enumerable.Repeat(c, sz - cur));
    }
  }
<<<<<<< HEAD
  public static void Resize<T>(this List<T> list, Int32 sz) where T : new() {
    Resize(list, sz, new T());
  }
  public static Vector3I DivFloor(Vector3 v, Single d) {
    return (Vector3I)(v / d).Floor();
  }
  public static Int32 DivFloor(Int32 n, Int32 d) {
    return n - Mod(n, d);
  }
  public static Vector3I DivFloor(Vector3I v, Int32 d) {
    return v - Mod(v, d);
  }
  public static Vector3I DivFloor(Vector3I v, Vector3I d) {
    return v - Mod(v, d);
  }
=======
  public static void Resize<T>(this List<T> list, int sz) where T : new() {
    Resize(list, sz, new T());
  }
>>>>>>> 82d442f (first commit:)
  public static Vector3I Mod(Vector3I v, Vector3I m) {
    var r = v % m;
    r.X = r.X < 0 ? r.X + m.X : r.X;
    r.Y = r.Y < 0 ? r.Y + m.Y : r.Y;
    r.Z = r.Z < 0 ? r.Z + m.Z : r.Z;
    return r;
  }
<<<<<<< HEAD
  public static Vector3I Mod(Vector3I v, Int32 m) {
=======
  public static Vector3I Mod(Vector3I v, int m) {
>>>>>>> 82d442f (first commit:)
    var r = v % m;
    r.X = r.X < 0 ? r.X + m : r.X;
    r.Y = r.Y < 0 ? r.Y + m : r.Y;
    r.Z = r.Z < 0 ? r.Z + m : r.Z;
    return r;
  }
<<<<<<< HEAD
  public static int ModFlat(Vector3I v, Int32 m) {
    return Flat(Mod(v, m), m);
  }
  public static int Mod(Int32 x, Int32 m) {
=======
  public static int ModFlat(Vector3I v, int m) {
    return Flat(Mod(v, m), m);
  }
  public static int Mod(int x, int m) {
>>>>>>> 82d442f (first commit:)
    int r = x % m;
    return r < 0 ? r + m : r;
  }
}
