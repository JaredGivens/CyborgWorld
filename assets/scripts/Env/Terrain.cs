using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
public class Terrain : IDisposable {
  private Chunk.Geometry[] _geometryMap;
  private Chunk.Save[] _saveMap;
  private Chunk.RegionHandle[] _regionMap;
  private MobSpawner _mobSpawner;
  private Vector3 _position;
  public volatile Int32 LoadedSaves = 0;
  private Chunk.DisplayOptions _options;
  private Rid _space;
  public volatile Int32 RunningTaskCount = 0;
  public Terrain(Rid scenario, Rid space) {
    _space = space;
    if (Glob.RD == null) {
      var computeThread = new Thread(() => {
        Glob.RD = RenderingServer.CreateLocalRenderingDevice();
        Chunk.Compute.Init();
      });
      computeThread.Start();
      computeThread.Join();
    }
    _geometryMap = new Chunk.Geometry[Chunk.Geometry.MapDimLen3];
    _saveMap = new Chunk.Save[Chunk.Save.MapDimLen3];
    _regionMap = new Chunk.RegionHandle[Chunk.Region.MapDimLen3];
    for (Int32 i = 0; i < Chunk.Save.MapDimLen3; ++i) {
      _saveMap[i] = new Chunk.Save(_regionMap, _geometryMap, space);
    }
    for (Int32 i = 0; i < Chunk.Region.MapDimLen3; ++i) {
      _regionMap[i] = new Chunk.RegionHandle();
    }
    for (Int32 i = 0; i < Chunk.Geometry.MapDimLen3; ++i) {
      _geometryMap[i] = new Chunk.Geometry(_saveMap, scenario, space);
    }
  }
  public void RunTask(Action act) {
    Interlocked.Increment(ref RunningTaskCount);
    Task.Run(() => {
      try {
        act();
      } catch (Exception e) {
        GD.PrintErr(e);
      } finally {
        Interlocked.Decrement(ref RunningTaskCount);
      }
    });
  }
  public void Load(Vector3 pos) {
    _position = pos;
    foreach (var save in _saveMap) {
      save.Load();
    }
    LoadedSaves = 0;
    foreach (var key in LoadKeys(PosGeoKey(_position))) {
      RunTask(() => {
        _saveMap[Glob.ModFlat2(key, Chunk.Save.MapDimLen)].StoreLoad(key);
        Interlocked.Increment(ref LoadedSaves);
      });
    }
  }

  public void Dispose() {
    Task.Run(() => {
      while (RunningTaskCount != 0) {
        Thread.Sleep(128);
      }
      foreach (var geo in _geometryMap) {
        geo.Dispose();
      }
      foreach (var save in _saveMap) {
        save.Store();
        save.Dispose();
      }
      foreach (var region in _regionMap) {
        region.Use(region => region.Store());
      }
    });
  }
  List<Vector3I> AllKeys(Vector3I key0) {
    List<Vector3I> allKeys = new();
    for (Int32 i = 0; i < Chunk.Geometry.MapDimLen; ++i) {
      for (Int32 j = 0; j < Chunk.Geometry.MapDimLen; ++j) {
        for (Int32 k = 0; k < Chunk.Geometry.MapDimLen; ++k) {
          var key1 = new Vector3I(i, j, k)
          - (Vector3I.One * (Chunk.Geometry.MapDimLen / 2));
          allKeys.Add(key1 + key0);
        }
      }
    }
    return allKeys;
  }
  List<Vector3I> LoadKeys(Vector3I key0) {
    List<Vector3I> loadKeys = new();
    for (Int32 i = 0; i < Glob.LoadDist; ++i) {
      for (Int32 j = 0; j < Glob.LoadDist; ++j) {
        for (Int32 k = 0; k < Glob.LoadDist; ++k) {
          var key1 = new Vector3I(i, j, k)
          - (Vector3I.One * (Glob.LoadDist / 2));
          loadKeys.Add(key1 + key0);
        }
      }
    }
    return loadKeys;
  }
  static Transform3D TsfAddSdfRange(Transform3D tsf) {
    var newScale = tsf.Basis.Scale + Vector3.One * Glob.SdfRange;
    return tsf.ScaledLocal(newScale / tsf.Basis.Scale);
  }
  public void ApplySdf(Aoe aoe, Transform3D tsf, Int16 blockId = -1) {
    var state = PhysicsServer3D.SpaceGetDirectState(_space);
    var shapeQuery = new PhysicsShapeQueryParameters3D();
    shapeQuery.ShapeRid = aoe.Shape.GetRid();
    shapeQuery.Transform = TsfAddSdfRange(tsf);
    shapeQuery.CollideWithBodies = false;
    shapeQuery.CollideWithAreas = true;
    var saveAreas = state.IntersectShape(shapeQuery, 16);
    GD.Print("using compute", saveAreas.Count);
    RunTask(() => {
      var saves = new List<Chunk.Save>();
      for (int i = 0; i < saveAreas.Count; ++i) {
        var rid = (Rid)saveAreas[i]["rid"];
        if (!Chunk.Save.RidMap.ContainsKey(rid)) {
          GD.PrintErr($"found {rid} in chunk save areas");
          continue;
        }
        saves.Add(Chunk.Save.RidMap[rid]);
      }
      Chunk.Compute.ApplySdf(saves, aoe, tsf, blockId);
    });
  }
  public (Chunk.BlockId, Memory<Int16>)? Interact(Vector3 pos) {
    var gkey = PosGeoKey(pos);
    var localPos = (Vector3I)pos - gkey * Chunk.Geometry.Size;
    return _saveMap[Glob.ModFlat2(gkey, Chunk.Save.MapDimLen)].Interact(localPos);
  }
  Vector3I PosGeoKey(Vector3 p) {
    return Glob.DivFloor(p - Vector3.One * 2, Chunk.Geometry.Size * Chunk.Geometry.Scale);
  }
  public void Process(Vector3 pos) {
    var oldKey = PosGeoKey(_position);
    var newKey = PosGeoKey(pos);
    if (Chunk.Geometry.Options != _options) {
      var keys = AllKeys(newKey);
      _position = pos;
      _options = Chunk.Geometry.Options;
      foreach (var key in keys) {
        RunTask(() => {
          var geo = _geometryMap[Glob.ModFlat2(key, Chunk.Geometry.MapDimLen)];
          geo.Update();
        });
      }
    } else if (oldKey != newKey) {
      _position = pos;
      var okeys = LoadKeys(oldKey);
      var nkeys = LoadKeys(newKey);
      foreach (var key in nkeys) {
        if (okeys.Contains(key)) {
          continue;
        }
        RunTask(() => {
          var save = _saveMap[Glob.ModFlat2(key, Chunk.Save.MapDimLen)];
          save.StoreLoad(key);
          //var geo = _geometryMap[Glob.ModFlat2(key, Chunk.Geometry.MapDimLen)];
          //geo.UpdateGrass(pos);
        });
      }
    }
  }
}
