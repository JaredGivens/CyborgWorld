using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace Chunk {
  [Flags]
  public enum DisplayOptions {
    None = 0,
    Boundries = 1,
    Normals = 2,
    Colors = 4,
  }
  // NOT THREAD SAFE LOCK BEFORE USE
  public class Geometry : IDisposable {
    public const Single Scale = 1;
    public const Int32 Size = 28;
    public const Int32 DimLen = 32;
    public const Int32 DimLen2 = DimLen * DimLen;
    public const Int32 DimLen3 = DimLen2 * DimLen;
    public const Int32 MapDimLen = 8;
    public const Int32 MapDimLen2 = MapDimLen * MapDimLen;
    public const Int32 MapDimLen3 = MapDimLen * MapDimLen2;
    private const Int32 _grassRadius = Size * 2;
    private static Int32[] _inQuadIndOffsets = {
          0, 1 + DimLen, 1,
          0, DimLen, 1 + DimLen,
          0, 1 + DimLen2, DimLen2,
          0, 1, 1 + DimLen2,
          0, DimLen + DimLen2, DimLen,
          0, DimLen2, DimLen + DimLen2,
        };
    private static Int32[] _outQuadIndOffsets = {
          0, 1 + DimLen, DimLen,
          0, 1, 1 + DimLen,
          0, 1 + DimLen2, 1,
          0, DimLen2, 1 + DimLen2,
          0, DimLen + DimLen2, DimLen2,
          0, DimLen, DimLen + DimLen2,
        };
    public static DisplayOptions Options;
    public static Vector3I BlockInd(Vector3I block) {
      return Glob.DivFloor(block, DimLen);
    }
    public static SByte CylinderSdf(Transform3D tsf, Vector3I worldCell) {

      var local = tsf.AffineInverse() * worldCell;
      var rd = new Vector2(local.X, local.Y).Length();
      var z = Math.Abs(local.Z);
      var d = (z, rd) switch
      {
        { z: <= 1 } => rd - 1,
        { z: > 1, rd: <= 1 } => z - 1,
        _ => MathF.Sqrt(MathF.Pow(rd - 1, 2)
            + MathF.Pow(MathF.Abs(local.Z) - 1, 2)),
      };
      return (SByte)Math.Clamp(
          Math.Round(d * Glob.DistFac), SByte.MinValue + 1, SByte.MaxValue);
    }
    public static SByte SphereSdf(Transform3D tsf, Vector3I worldCell) {
      var d = (tsf.AffineInverse() * worldCell).Length() - 1;
      return (SByte)Math.Clamp(
          Math.Round(d * Glob.DistFac), SByte.MinValue + 1, SByte.MaxValue);
    }
    private StandardMaterial3D _debugMat = new();
    private Save[] _saveMap;
    public Vector3I Gkey = Vector3I.MaxValue;
    private List<Int32> _vertFlats = new();
    private List<Vector3> _cellVerts = new();
    private List<Vector3> _verts = new();
    private List<Vector3> _norms = new();
    private List<Vector2> _uvs = new();
    private MeshInstance3D _mesh = new();
    private List<Single> _grassTsfs = new();
    private Godot.Collections.Array _vertBufs = new();
    private Godot.Collections.Array _boundtryVertBufs = new();
    private Godot.Collections.Array _normalVertBufs = new();
    private ArrayMesh _arrMesh;
    private Rid _inst = new();
    private ConcavePolygonShape3D _hullShape = new();
    private StaticBody3D _body = new();
    private DualContour _dualContour;
    private Save _locked;
    private MultiMesh _grassMulti = new();
    private Rid _grassMultiInst;
    private Transform3D _tsf;
    public Geometry(Save[] saveMap, Rid scenario, Rid space) {
      _debugMat.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
      _saveMap = saveMap;
      _dualContour = new(GetCell);
      _vertBufs.Resize((Int32)Mesh.ArrayType.Max);
      _boundtryVertBufs.Resize((Int32)Mesh.ArrayType.Max);
      _normalVertBufs.Resize((Int32)Mesh.ArrayType.Max);
      _arrMesh = new ArrayMesh();
      _body.CollisionMask = 0;
      _inst = RenderingServer.InstanceCreate2(_arrMesh.GetRid(), scenario);
      RenderingServer.InstanceGeometrySetCastShadowsSetting(_inst,
          RenderingServer.ShadowCastingSetting.Off);
      PhysicsServer3D.BodySetSpace(_body.GetRid(), space);

      RenderingServer.MultimeshSetMesh(_grassMulti.GetRid(), Loader.GrassMesh.GetRid());
      _grassMulti.UseColors = false;
      _grassMulti.UseCustomData = false;
      _grassMultiInst = RenderingServer.InstanceCreate2(_grassMulti.GetRid(), scenario);
      RenderingServer.InstanceGeometrySetCastShadowsSetting(_grassMultiInst,
          RenderingServer.ShadowCastingSetting.Off);
    }
    public static Vector3I Skey(Vector3I gkey) { return gkey; }
    Cell GetCell(Vector3I dcell) {
      return (Cell)_saveMap[Glob.ModFlat2(Skey(Gkey), Save.MapDimLen)]
        .Durable.Cells[Glob.Flat(dcell, DimLen)];
    }
    public void AddVertices() {
      var normVerts = new List<Vector3>();
      for (Int32 i = 1; i < DimLen - 2; ++i) {
        for (Int32 j = 1; j < DimLen - 2; ++j) {
          for (Int32 k = 1; k < DimLen - 2; ++k) {
            var dcell = new Vector3I(i, j, k);
            var res = _dualContour.ComputeVert(dcell, Gkey);
            //if (Options.HasFlag(DisplayOptions.Normals)) {
            _dualContour.AddDebug(normVerts);
            //}
            if (res is Vector3 v) {
              _vertFlats.Add(i * DimLen2 + j * DimLen + k);
              _cellVerts.Add(v);
            }
          }
        }
      }
      //if (Options.HasFlag(DisplayOptions.Normals) && verts.Count != 0) {
      _normalVertBufs[(Int32)Mesh.ArrayType.Vertex] = normVerts.ToArray();
      //}
    }
    void Hide() {
      RenderingServer.InstanceSetVisible(_inst, false);
      RenderingServer.InstanceSetVisible(_grassMultiInst, false);
      PhysicsServer3D.BodyClearShapes(_body.GetRid());
    }
    public void Rebuild(Vector3I gkey) {
      Gkey = gkey;
      _tsf = new Transform3D(
          Basis.FromScale(Vector3.One * Scale),
         (Vector3)Gkey * Size * Scale);
      var color = (Vector3)Gkey % 4 / 4;
      _vertFlats.Clear();
      _cellVerts.Clear();
      _verts.Clear();
      _norms.Clear();
      _uvs.Clear();
      _grassTsfs.Clear();
      AddVertices();
      if (_vertFlats.Count == 0) {
        Hide();
        return;
      }
      StitchQuads();
      Update();
    }
    public void Update() {
      if (_verts.Count == 0) {
        Hide();
        return;
      }
      if (Options.HasFlag(DisplayOptions.Colors)) {
        _debugMat.AlbedoColor = Color.FromHsv(
            Glob.ModFlat2(Gkey, 2) / 8.0f, 0.5f, 0.6f);
      } else {
        _debugMat.AlbedoColor = new Color(1, 0, 1);
      }
      RenderingServer.InstanceSetVisible(_inst, true);
      RenderingServer.InstanceSetTransform(_inst, _tsf);
      _vertBufs[(Int32)Mesh.ArrayType.Normal] = _norms.ToArray();
      _vertBufs[(Int32)Mesh.ArrayType.Vertex] = _verts.ToArray();
      _vertBufs[(Int32)Mesh.ArrayType.TexUV] = _uvs.ToArray();
      _arrMesh.ClearSurfaces();
      _arrMesh.AddSurfaceFromArrays(
          Mesh.PrimitiveType.Triangles, _vertBufs);
      RenderingServer
        .InstanceSetSurfaceOverrideMaterial(_inst, 0,
            Options.HasFlag(DisplayOptions.Colors)
            ? _debugMat.GetRid()
            : Loader.TerrainShaderMat.GetRid());
      _hullShape = _arrMesh.CreateTrimeshShape();
      if (Options.HasFlag(DisplayOptions.Boundries)) {
        AddBoundries();
      }
      if (Options.HasFlag(DisplayOptions.Normals)) {
        var i = _arrMesh.GetSurfaceCount();
        _arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, _normalVertBufs);
        RenderingServer.MeshSurfaceSetMaterial(
            _arrMesh.GetRid(), i, _debugMat.GetRid());
      }
      PhysicsServer3D.BodyClearShapes(_body.GetRid());
      PhysicsServer3D.BodyAddShape(_body.GetRid(), _hullShape.GetRid(), _tsf);
      RenderingServer.MultimeshAllocateData(_grassMulti.GetRid(),
      _grassTsfs.Count / 12,
      RenderingServer.MultimeshTransformFormat.Transform3D);
      RenderingServer.InstanceSetVisible(_grassMultiInst, _grassTsfs.Count != 0);
      if (_grassTsfs.Count != 0) {
        RenderingServer.MultimeshSetBuffer(_grassMulti.GetRid(), _grassTsfs.ToArray());
        RenderingServer.InstanceSetTransform(_grassMultiInst, _tsf);
        RenderingServer.InstanceGeometrySetMaterialOverride(_grassMultiInst, Loader.GrassShaderMat.GetRid());
      }
    }
    public void UpdateGrass(Vector3 pos) {
      //if (pos.DistanceTo(Gkey * Size) > _grassRadius) {
      //RenderingServer.InstanceSetVisible(_grassMultiInst, false);
      //return;
      //}
    }
    void AddBoundries() {
      _boundtryVertBufs[(Int32)Mesh.ArrayType.Vertex] = new Vector3[]{
            new Vector3(0, 0, 0)* Size,
            new Vector3(1, 0, 0)* Size,
            new Vector3(0, 1, 0)* Size,
            new Vector3(1, 1, 0)* Size,
            new Vector3(0, 0, 1)* Size,
            new Vector3(1, 0, 1)* Size,
            new Vector3(0, 1, 1)* Size,
            new Vector3(1, 1, 1) * Size
          };
      _boundtryVertBufs[(Int32)Mesh.ArrayType.Index] = new Int32[] {
              0, 1,
              0, 2,
              0, 4,
              1, 3,
              2, 3,
              4, 5,
              4, 6,
              7, 6,
              7, 5,
              7, 3
            };
      var i = _arrMesh.GetSurfaceCount();
      _arrMesh.AddSurfaceFromArrays(
          Mesh.PrimitiveType.Lines, _boundtryVertBufs);
      RenderingServer
        .InstanceSetSurfaceOverrideMaterial(_inst, i, _debugMat.GetRid());
    }
    public void TryAddGrass(Int32 vertsOffset, Vector3 normal, Int32 seed) {
      var a = _verts[vertsOffset];
      var b = _verts[vertsOffset + 1];
      var c = _verts[vertsOffset + 2];
      // Compute two basis vectors for the plane
      Vector3 u = b - a;
      Vector3 v = c - a;

      // Generate random barycentric coordinates
      seed += Glob.Flat((Vector3I)(a * Size), Size) + Glob.ModFlat2(Gkey, 256);
      var rng = new Random(seed);

      var r1 = rng.NextSingle();
      var r2 = rng.NextSingle();

      // Ensure the random values sum to less than or equal to 1
      if (r1 + r2 > 1.0f) {
        r1 = 1.0f - r1;
        r2 = 1.0f - r2;
      }

      // Calculate the random point on the plane
      var tangent = Vector3.Up.Cross(normal);
      var pos = a + r1 * u + r2 * v;
      var basis = new Basis(tangent, MathF.Asin(tangent.Length()));
      basis = basis.Rotated(normal, rng.NextSingle() * MathF.Tau);
      basis = basis.Scaled(Vector3.One * ((rng.NextSingle() * 0.5f) + 0.5f));
      //GD.PrintS(pos);
      var tsf = new Transform3D(basis, pos);
      for (Int32 i = 0; i < 3; ++i) {
        for (Int32 j = 0; j < 4; ++j) {
          _grassTsfs.Add(tsf[j][i]);
        }
      }
    }
    void StitchQuad(Int32 dflat, Vector3I cell, Int32 side) {
      var quadIndOffsets = side < 3
        ? Geometry._outQuadIndOffsets
        : Geometry._inQuadIndOffsets;
      side %= 3;
      for (Int32 i = 0; i < 6; ++i) {
        _verts.Add(_cellVerts[
            _vertFlats.IndexOf(dflat - quadIndOffsets[side * 6 + i])
        ]);
      }
      var blockId = GetCell(cell).Id;
      _uvs.AddRange(Enumerable.Repeat(Loader.BlockUvs[blockId], 6));
      var vc = _verts.Count - 6;
      for (Int32 i = 0; i < 2; ++i) {
        var o = i * 3;
        var a = _verts[vc + o];
        var b = _verts[vc + o + 1];
        var c = _verts[vc + o + 2];
        var norm = Normal(a, b, c);
        _norms.AddRange(Enumerable.Repeat(norm, 3));
        if (blockId == (Byte)BlockId.Grass) {
          var normDot = Vector3.Up.Dot(norm);
          if (normDot > 0.9f) {
            for (Int32 j = 0; j < (normDot - 0.9f) * 10 * 20; ++j) {
              TryAddGrass(vc + o, norm, j);
            }
          }
        }
      }
    }
    Vector3 Normal(Vector3 a, Vector3 b, Vector3 c) {
      return (a - b).Cross(c - b).Normalized();
    }
    void StitchQuads() {
      foreach (Int32 dflat in _vertFlats) {
        var c0 = Glob.Unflat(dflat, DimLen);
        for (Int32 i = 0; i < 3; ++i) {
          if (c0.X <= 1 || c0.Y <= 1 || c0.Z <= 1) {
            continue;
          }
          var d0 = GetCell(c0).Dist;
          var c1 = c0;
          c1[i] += 1;
          var d1 = GetCell(c1).Dist;
          if (d0 == -128 || d1 == -128) {
            continue;
          }
          if (d0 < 0 && d1 >= 0) {
            StitchQuad(dflat, c0, i);
          } else if (d0 >= 0 && d1 < 0) {
            StitchQuad(dflat, c1, i + 3);
          }
        }
      }
    }
    public void Dispose() {
      _arrMesh.ClearSurfaces();
      RenderingServer.FreeRid(_inst);
      RenderingServer.FreeRid(_grassMultiInst);
      PhysicsServer3D.BodySetSpace(_body.GetRid(), new Rid());
    }
  }
}
