using Godot;
using System;
using System.Collections.Concurrent;
public enum AoeShape {
  Cube,
  Cylinder,
  Sphere,
  Count
}
public class Aoe : IDisposable {
  private static ConcurrentBag<Aoe>[] _bags = {
    new(), new(), new()
  };
  public static ulong[] Last = new ulong[(int)AoeShape.Count];
  private const int _poolCount = 32;
  public static Aoe GetAoe(AoeShape aoeShape) {
    if (!_bags[(int)aoeShape].TryTake(out Aoe res)) {
      GD.PrintErr(aoeShape, "bag grew");
      res = new Aoe(aoeShape);
    }
    return res;
  }
  public static void InitAoes() {
    for (int i = 0; i < (int)AoeShape.Count; ++i) {
      _bags[i].Clear();
    }
    for (int i = 0; i < _poolCount; ++i) {
      var aoe = new Aoe(AoeShape.Sphere);
      _bags[(int)AoeShape.Sphere].Add(aoe);
      Last[(int)AoeShape.Sphere] = aoe.Shape.GetRid().Id;
    }
    for (int i = 0; i < _poolCount; ++i) {
      var aoe = new Aoe(AoeShape.Cube);
      _bags[(int)AoeShape.Cube].Add(aoe);
      Last[(int)AoeShape.Cube] = aoe.Shape.GetRid().Id;
    }
    for (int i = 0; i < _poolCount; ++i) {
      var aoe = new Aoe(AoeShape.Cylinder);
      _bags[(int)AoeShape.Cylinder].Add(aoe);
      Last[(int)AoeShape.Cylinder] = aoe.Shape.GetRid().Id;
    }
  }
  public Area3D DamageArea = new();
  public Shape3D Shape;
  public AoeShape AoeShape;
  public Aoe(AoeShape aoeShape) {
    AoeShape = aoeShape;
    // may have to modify damage area for persist effects
    DamageArea.CollisionLayer = 0;
    DamageArea.CollisionMask = 4;
    DamageArea.Monitoring = false;
    switch (aoeShape) {
      case AoeShape.Cube:
      var b = new BoxShape3D();
      b.Size = Vector3.One * 2;
      Shape = b;
      break;
      case AoeShape.Cylinder:
      var c = new CylinderShape3D();
      c.Radius = 1;
      Shape = c;
      break;
      case AoeShape.Sphere:
      var s = new SphereShape3D();
      s.Radius = 1;
      Shape = s;
      break;
    }
    //PhysicsServer3D.AreaSetSpace(DamageArea.GetRid(), Glob.Space);
    //PhysicsServer3D.AreaAddShape(SdfArea.GetRid(), Shape.GetRid());
    //PhysicsServer3D.AreaAddShape(DamageArea.GetRid(), Shape.GetRid());
  }
  public void Dispose() {
    _bags[(int)AoeShape].Add(this);
  }
}
