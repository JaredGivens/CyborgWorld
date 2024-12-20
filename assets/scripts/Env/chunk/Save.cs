using Godot;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Chunk {
  public enum BlockId : Int16 {
    None = -1,
    Grass,
    Dirt,
    Stone,
    Scanner,
    Cement,
    Chest,
    Count,
  }
  [Flags]
  public enum BlockProp {
    None = 0,
    Inventory = 1,
  }
  public enum BlockMapping {
    Uniform,
    TopFrontN,
    TopFrontS,
    TopFrontE,
    TopFrontW,
  }
  [Flags]
  enum Status {
    None = 0,
    Loaded = 1,
  };
  [StructLayout(LayoutKind.Explicit)]
  public struct Cell {
    [FieldOffset(0)] private Int32 _int;
    [FieldOffset(0)] public SByte Dist;
    [FieldOffset(1)] public Byte Id;
    [FieldOffset(2)] public SByte Theta;
    [FieldOffset(3)] public Byte Phi;
    public static explicit operator Cell(Int32 i) => new Cell { _int = i };
    public static explicit operator Int32(Cell c) => c._int;
    public void SetNormal(Vector3 n) {
      Single phi = MathF.Acos(n.Y); // z = cos(phi), so phi = acos(z)
      Single theta = MathF.Atan2(n.Z, n.X); // theta = atan2(y, x)
      var thetai = (Int32)MathF.Round(theta / MathF.PI * 128);
      var phii = (Int32)MathF.Round(phi / MathF.PI * 255);
      thetai = thetai == 128 ? -128 : thetai;
      Theta = (SByte)thetai;
      Phi = (Byte)phii;
    }
    public Vector3 GetNormal() {
      var theta = ((Single)Theta) * MathF.PI / 128;
      var phi = ((Single)Phi) * MathF.PI / 255;
      return new Vector3(
      MathF.Sin(phi) * MathF.Cos(theta),
       MathF.Cos(phi),
      MathF.Sin(phi) * MathF.Sin(theta));
    }
  }
  public struct Durable {
    public readonly Int32[] Cells = new Int32[Geometry.DimLen3];
    public ItemMap Items = new();
    public Durable() { }
  }
  public class Save : IDisposable {
    public static BlockProp[] BlockProps;
    public const Int32 MapDimLen = Geometry.MapDimLen;//* 2;
    public const Int32 MapDimLen2 = MapDimLen * MapDimLen;
    public const Int32 MapDimLen3 = MapDimLen * MapDimLen2;
    public static Dictionary<Rid, Save> RidMap = new();
    private BoxShape3D _boxShape = new();
    private readonly ReaderWriterLockSlim _rwlock = new();
    private readonly Durable[] _durables = new Durable[2];
    private Int32 _durableI = 0;
    public Durable Durable => _durables[_durableI];
    private RegionHandle[] _regionMap;
    private Geometry[] _displayMap;
    private Area3D _boxArea = new();
    private Int32 Rflat;
    private Vector3I Rkey;
    public Vector3I Skey = Vector3I.MaxValue;
    private Gen _gen;
    private Status _status = Status.None;
    private Compute _compute = new();
    void Init() {
      BlockProps = new BlockProp[(Int32)BlockId.Count];
      BlockProps[(Int32)BlockId.Chest] = BlockProp.Inventory;
      BlockProps[(Int32)BlockId.Dirt] = BlockProp.None;
      BlockProps[(Int32)BlockId.Grass] = BlockProp.None;
      BlockProps[(Int32)BlockId.Stone] = BlockProp.None;
      BlockProps[(Int32)BlockId.Cement] = BlockProp.None;
      BlockProps[(Int32)BlockId.Scanner] = BlockProp.None;
    }
    public Save(RegionHandle[] regionMap, Geometry[] displayMap, Rid space) {
      if (BlockProps == null) {
        Init();
      }
      _durables[0] = new();
      _durables[1] = new();
      _regionMap = regionMap;
      _displayMap = displayMap;
      _boxArea.Monitoring = false;
      _boxArea.Monitorable = false;
      _boxArea.CollisionMask = 0;
      _boxArea.CollisionLayer = 2;
      _boxShape.Size = Vector3.One * Geometry.DimLen;
      PhysicsServer3D.AreaSetSpace(_boxArea.GetRid(), space);
      PhysicsServer3D.AreaAddShape(_boxArea.GetRid(), _boxShape.GetRid());
      PhysicsServer3D.AreaSetShapeDisabled(_boxArea.GetRid(), 0, true);
      RidMap.Add(_boxArea.GetRid(), this);
    }
    public void Load() {
      _gen = new(Glob.Save.Seed);
    }
    public (Chunk.BlockId, Memory<Int16>)? Interact(Vector3I localPos) {
      var cell = Glob.Mod(localPos, Geometry.Size);
      var celli = Glob.Flat(cell, Geometry.DimLen);
      var blockId = (BlockId)((Cell)Durable.Cells[celli]).Id;
      if (!BlockProps[(Int32)blockId].HasFlag(BlockProp.Inventory)) {
        return null;
      }
      var items = Durable.Items[(Int16)celli];
      return (blockId, items);
    }
    public void PrintDists() {
      //var s = "";
      //var s1 = "";
      var s2 = "";
      for (var i = 0; i < Geometry.DimLen; ++i) {
        for (var j = 0; j < Geometry.DimLen; ++j) {
          var c = ((Cell)Durable.Cells[i * Geometry.DimLen + j * Geometry.DimLen2 + 20]);
          //var n = c.GetNormal();
          //s += $"{n.X.ToString("f1")} {n.Y.ToString("f1")} {n.Z.ToString("f1")}|".PadLeft(15);
          //s1 += $"{c.Theta} {c.Phi}|".PadLeft(12);
          s2 += c.Dist.ToString().PadLeft(4);
        }
        //s += "\n";
        //s1 += "\n";
        s2 += "\n";
      }
      //GD.Print(s);
      //GD.Print(s1);
      GD.Print(s2);
    }

    public Save StoreLoad(Vector3I skey) {
      _rwlock.EnterReadLock();
      try {
        if (Skey == skey) {
          return this;
        }
      } catch { throw; } finally {
        _rwlock.ExitReadLock();
      }
      _rwlock.EnterUpgradeableReadLock();
      try {
        if (Skey == skey) {
          return this;
        }
        _rwlock.EnterWriteLock();
        try {
          Store();
          var rflat = Glob.ModFlat2(skey, Region.DimLen);
          var rkey = Glob.DivFloor(skey, Region.DimLen);
          var found = false;
          _durables[_durableI ^ 1].Items = new();
          _regionMap[Glob.ModFlat2(rkey, Region.MapDimLen)].StoreLoad(rkey, region => {
            if (region.HasChunk(rflat)) {
              region.GetChunk(rflat, ref _durables[_durableI ^ 1]);
              //PrintDists();
              found = true;
            }
          });
          if (!found) {
            _gen.GenSdf(skey, ref _durables[_durableI ^ 1]);
          }
          _status |= Status.Loaded;
          Skey = skey;
          Rkey = rkey;
          Rflat = rflat;
          _durableI ^= 1;
          _compute.UpdateCellBuf(Durable.Cells);
          var scale = Vector3.One * Geometry.Scale;
          var origin = (Vector3.One * 0.5f * Geometry.DimLen + Skey * Geometry.Size)
            * Geometry.Scale;
          Transform3D tsf = new Transform3D(Basis.FromScale(scale), origin);
          PhysicsServer3D.AreaSetShapeTransform(_boxArea.GetRid(), 0, tsf);
          PhysicsServer3D.AreaSetShapeDisabled(_boxArea.GetRid(), 0, false);
        } catch { throw; } finally {
          RebuildGeometry();
          _rwlock.ExitWriteLock();
        }
      } catch { throw; } finally {
        _rwlock.ExitUpgradeableReadLock();
      }

      return this;
    }
    public Godot.Collections.Array<RDUniform> GetPUniforms(Transform3D tsf, Int16 blockId) {
      _rwlock.EnterWriteLock();
      _compute.UpdateUniformBuf(tsf, Skey * Geometry.Size, blockId);
      return _compute.PUniforms;
    }
    public void ComputeUpdate() {
      var bytes = _compute.GetCellBuffer();
      Buffer.BlockCopy(bytes, 0, Durable.Cells, 0, Geometry.DimLen3 * sizeof(Int32));
      RebuildGeometry();
      _rwlock.ExitWriteLock();
    }

    private void RebuildGeometry() {
      var disp = _displayMap[Glob.ModFlat2(Skey, Geometry.MapDimLen)];
      lock (disp) {
        disp.Rebuild(Skey);
      }
    }

    void FromPadded(Byte[] paddedCells) {
      for (Int32 i = 0; i < Geometry.DimLen; ++i) {
        for (Int32 j = 0; j < Geometry.DimLen; ++j) {
          Buffer.BlockCopy(paddedCells,
              (Gen.CDimLen2 * (Glob.SdfRange + i)
              + Gen.CDimLen * (Glob.SdfRange + j) + Glob.SdfRange) * sizeof(Int32),
              _durables[_durableI].Cells, (Geometry.DimLen2 * i + Geometry.DimLen * j) * sizeof(Int32),
              Geometry.DimLen * sizeof(Int32));
        }
      }
    }

    public void Store() {
      if (!_status.HasFlag(Status.Loaded)) {
        return;
      }
      _regionMap[Glob.ModFlat2(Rkey, Region.MapDimLen)].StoreLoad(Rkey, region => {
        region.SetChunk(Rflat, ref _durables[_durableI]);
      });
    }

    public void Dispose() {
      PhysicsServer3D.AreaSetSpace(_boxArea.GetRid(), new Rid());
    }
  }
}
