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
    foreach (var key in GeoKeys(PosGeoKey(_position))) {
      RunTask(() => {
        _saveMap[Glob.ModFlat(key, Chunk.Save.MapDimLen)].StoreLoad(key);
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
  List<Vector3I> GeoKeys(Vector3I dkey0) {
    List<Vector3I> geoKeys = new();
    for (Int32 i = 0; i < Chunk.Geometry.MapDimLen3; ++i) {
      var dkey1 = Glob.Unflat(i, Chunk.Geometry.MapDimLen)
        - (Vector3I.One * (Chunk.Geometry.MapDimLen / 2));
      geoKeys.Add(dkey1 + dkey0);
    }
    return geoKeys;
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
  public (Chunk.BlockId, Int16[]) Interact(Vector3 pos) {
    var gkey = PosGeoKey(pos);
    return _saveMap[Glob.ModFlat(gkey, Chunk.Save.MapDimLen)].Interact(pos);
  }
  Vector3I PosGeoKey(Vector3 p) {
    return Glob.DivFloor(p, Chunk.Geometry.Size * Chunk.Geometry.Scale);
  }
  public void Process(Vector3 pos) {
    var oldKey = PosGeoKey(_position);
    var newKey = PosGeoKey(pos);
    if (Chunk.Geometry.Options != _options) {
      var gkeys = GeoKeys(newKey);
      _position = pos;
      _options = Chunk.Geometry.Options;
      foreach (var gkey in gkeys) {
        RunTask(() => {
          var geo = _geometryMap[Glob.ModFlat(gkey, Chunk.Geometry.MapDimLen)];
          geo.Update();
        });
      }
    } else if (oldKey != newKey) {
      _position = pos;
      var odkeys = GeoKeys(oldKey);
      var ndkeys = GeoKeys(newKey);
      foreach (var skey in ndkeys) {
        if (odkeys.Contains(skey)) {
          continue;
        }
        RunTask(() => {
          var save = _saveMap[Glob.ModFlat(skey, Chunk.Save.MapDimLen)];
          save.StoreLoad(skey);
        });
      }
    }
  }
}
