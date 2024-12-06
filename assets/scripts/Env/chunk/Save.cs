using Godot;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Chunk {
  public enum BlockId {
    Grass,
    Cement,
    Stone,
    Scanner,
    Dirt,
    Chest,
    Count,
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
    public readonly Int16[] Items = new Int16[Geometry.DimLen3 * 16];
    public Durable() { }
  }
  public class Save : IDisposable {
    public const Int32 MapDimLen = 8;
    public const Int32 MapDimLen2 = MapDimLen * MapDimLen;
    public const Int32 MapDimLen3 = MapDimLen * MapDimLen2;
    public static Dictionary<Rid, Save> RidMap = new();
    private BoxShape3D _boxShape = new();
    private readonly ReaderWriterLockSlim _rwlock = new();
    public Durable Durable = new();
    private RegionHandle[] _regionMap;
    private Geometry[] _displayMap;
    private Area3D _boxArea = new();
    private Int32 Rflat;
    private Vector3I Rkey;
    public Vector3I Skey = Vector3I.MaxValue;
    private Gen _gen;
    private Status _status = Status.None;
    private Compute _compute = new();
    public Save(RegionHandle[] regionMap, Geometry[] displayMap, Rid space) {
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
    public (Chunk.BlockId, Int16[]) Interact(Vector3 pos) {
      var itemi = Glob.Flat((Vector3I)pos.Floor(), Geometry.DimLen * 16);
      var celli = Glob.Flat((Vector3I)pos.Floor(), Geometry.DimLen);
      Int16[] result = new Int16[16];
      Array.Copy(Durable.Items, itemi, result, 0, 16);
      return ((BlockId)((Cell)Durable.Cells[celli]).Id, result);
    }

    public Save StoreLoad(Vector3I skey) {
      _rwlock.EnterReadLock();
      if (Skey == skey) {
        _rwlock.ExitReadLock();
        return this;
      }
      _rwlock.ExitReadLock();
      _rwlock.EnterUpgradeableReadLock();
      if (Skey == skey) {
        _rwlock.ExitUpgradeableReadLock();
        return this;
      }
      _rwlock.EnterWriteLock();
      Store();
      _status |= Status.Loaded;
      Skey = skey;
      Rflat = Glob.ModFlat(Skey, Region.DimLen);
      Rkey = Glob.DivFloor(Skey, Region.DimLen);
      var found = false;
      _regionMap[Glob.ModFlat(Rkey, Region.MapDimLen)].StoreLoad(Rkey, region => {
        if (region.HasChunk(Rflat)) {
          region.GetChunk(Rflat, ref Durable);
          found = true;
        }
      });
      if (!found) {
        FromPadded(_gen.GenSdf(Skey));

        //var s = "";
        //var s1 = "";
        //var s2 = "";
        //for (var i = 0; i < Geometry.DimLen; ++i) {
        //for (var j = 0; j < Geometry.DimLen; ++j) {
        //var c = ((Cell)Cells[i * Geometry.DimLen + j * Geometry.DimLen2 + 20]);
        //var n = c.Normal();
        //s += $"{n.X.ToString("f1")} {n.Y.ToString("f1")} {n.Z.ToString("f1")}|".PadLeft(15);
        //s1 += $"{c.Theta} {c.Phi}|".PadLeft(12);
        //s2 += c.Dist.ToString().PadLeft(4);
        //}
        //s += "\n";
        //s1 += "\n";
        //s2 += "\n";
        //}
        //GD.Print(s);
        //GD.Print(s1);
        //GD.Print(s2);
      }
      _compute.UpdateCellBuf(Durable.Cells);
      var scale = Vector3.One * Geometry.Scale;
      var origin = (Vector3.One * 0.5f * Geometry.DimLen + Skey * Geometry.Size)
        * Geometry.Scale;
      Transform3D tsf = new Transform3D(Basis.FromScale(scale), origin);
      PhysicsServer3D.AreaSetShapeTransform(_boxArea.GetRid(), 0, tsf);
      PhysicsServer3D.AreaSetShapeDisabled(_boxArea.GetRid(), 0, false);
      _rwlock.ExitWriteLock();
      _rwlock.ExitUpgradeableReadLock();
      RebuildGeometry();
      return this;

    }
    public Godot.Collections.Array<RDUniform> GetPUniforms(Transform3D tsf, Int16 blockId) {
      _compute.UpdateUniformBuf(tsf, Skey * Geometry.Size, blockId);
      return _compute.PUniforms;
    }
    public void ComputeUpdate() {
      _rwlock.EnterWriteLock();
      var bytes = _compute.GetCellBuffer();
      Buffer.BlockCopy(bytes, 0, Durable.Cells, 0, Geometry.DimLen3 * sizeof(Int32));
      var s = "";
      var s2 = "";
      for (var i = 0; i < Geometry.DimLen; ++i) {
        for (var j = 0; j < Geometry.DimLen; ++j) {
          var c = ((Cell)Durable.Cells[i * Geometry.DimLen + j * Geometry.DimLen2 + 20]);
          var n = c.GetNormal();
          s += $"{n.X.ToString("f1")} {n.Y.ToString("f1")} {n.Z.ToString("f1")}|".PadLeft(15);
          s2 += c.Dist.ToString().PadLeft(4);
        }
        s += "\n";
        s2 += "\n";
      }
      GD.Print(s2);
      _rwlock.ExitWriteLock();
      RebuildGeometry();
    }

    private void RebuildGeometry() {
      _rwlock.EnterReadLock();
      var disp = _displayMap[Glob.ModFlat(Skey, Geometry.MapDimLen)];
      lock (disp) {
        disp.Rebuild(Skey);
      }
      _rwlock.ExitReadLock();
    }

    void FromPadded(Cell[] paddedCells) {
      unsafe {
        fixed (Cell* src = paddedCells)
        fixed (int* dest = Durable.Cells) {
          Buffer.MemoryCopy(src, dest,
              Geometry.DimLen3 * sizeof(Int32),
              Geometry.DimLen3 * sizeof(Int32));
        }
      }
    }
    void FromPadded(Byte[] paddedCells) {
      for (Int32 i = 0; i < Geometry.DimLen; ++i) {
        for (Int32 j = 0; j < Geometry.DimLen; ++j) {
          Buffer.BlockCopy(paddedCells,
              (Gen.CDimLen2 * (Glob.SdfRange + i)
              + Gen.CDimLen * (Glob.SdfRange + j) + Glob.SdfRange) * sizeof(Int32),
              Durable.Cells, (Geometry.DimLen2 * i + Geometry.DimLen * j) * sizeof(Int32),
              Geometry.DimLen * sizeof(Int32));
        }
      }
    }

    public void Store() {
      if (!_status.HasFlag(Status.Loaded)) {
        return;
      }
      _regionMap[Glob.ModFlat(Rkey, Region.MapDimLen)].StoreLoad(Rkey, region => {
        region.SetChunk(Rflat, ref Durable);
      });
    }

    public void Dispose() {
      PhysicsServer3D.AreaSetSpace(_boxArea.GetRid(), new Rid());
    }
  }
}
