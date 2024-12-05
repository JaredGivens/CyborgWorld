using Godot;
using System;
using System.Buffers;
using System.Collections.Generic;
using Google.FlatBuffers;
namespace Chunk {
  record struct Fill(Int32 Index, Cell cell);
  enum Prefabs {
    Ruin1,
    Ruin2,
    Ruin3,
    Ruin5,
    Ruin6,
    Ruin7,
    Ruin8,
    Ruin9,
    Ruin10,
    Ruin14,
    Ruin16,
    Count
  }
  public class Gen {
    private static String[] _prefabFiles = {
    "res://models/sdfs/Ruin1.fb",
    "res://models/sdfs/Ruin2.fb",
    "res://models/sdfs/Ruin3.fb",
    "res://models/sdfs/Ruin5.fb",
    "res://models/sdfs/Ruin6.fb",
    "res://models/sdfs/Ruin7.fb",
    "res://models/sdfs/Ruin8.fb",
    "res://models/sdfs/Ruin9.fb",
    "res://models/sdfs/Ruin10.fb",
    "res://models/sdfs/Ruin14.fb",
    "res://models/sdfs/Ruin16.fb",
    };
    private static Fb.Prefab[] _prefabs;
    private static void Init() {
      if (_prefabs != null) {
        return;
      }
      _prefabs = new Fb.Prefab[(Int32)Prefabs.Count];
      for (Int32 i = 0; i < _prefabFiles.Length; ++i) {
        _prefabs[i] = Fb.Prefab.GetRootAsPrefab(
            new ByteBuffer(FileAccess.GetFileAsBytes(_prefabFiles[i])));
      }
      //var s = "";
      //for (Int32 k = 0; k < pf.Depth; ++k) {
      //for (Int32 i = pf.Height - 1; i > -1; --i) {
      //for (Int32 j = 0; j < pf.Width; ++j) {
      //var c = ((Cell)pf.Cells(j * pf.Height * pf.Depth + i * pf.Depth + k)).GetNormal();
      //s += $"{c.X.ToString("f1")} {c.Y.ToString("f1")} {c.Z.ToString("f1")}|".ToString().PadLeft(15);
      //}
      //s += '\n';
      //}
      //s += '\n';
      //}
      //GD.Print(s);
    }
    public const Int32 CDimLen = Geometry.DimLen;
    public const Int32 CDimLen2 = CDimLen * CDimLen;
    public const Int32 CDimLen3 = CDimLen2 * CDimLen;
    public const Int32 NDimLen = Geometry.DimLen + 2;
    public const Int32 NDimLen2 = NDimLen * NDimLen;
    public const Int32 NDimLen3 = NDimLen2 * NDimLen;
    private static readonly Vector3I[] Neighbors = {
      Vector3I.Up, Vector3I.Back, Vector3I.Right,
      Vector3I.Down, Vector3I.Left, Vector3I.Forward,
    };
    static private Rid _jfaShader = new();
    private readonly FastNoiseLite _valueNoise = new();
    private readonly FastNoiseLite _simplexNoise0;
    private readonly FastNoiseLite _simplexNoise1;
    private readonly PriorityQueue<Fill, Single> _fills = new();
    private readonly Cell[] _cells;
    private readonly Single[] _noise;
    private Vector3I _pos;
    private Vector3I _skey;
    private Rid _cellBuf;
    private Rid _noiseBuf;
    private readonly Godot.Collections.Array<RDUniform> _pUniforms = new();
    private Rid _uniformSet;
    private Rid _pipeline;
    public Gen(Int32 seed) {
      if (!_jfaShader.IsValid) {
        Init();
        var file = GD.Load<RDShaderFile>("res://shaders/jfa.glsl");
        var bytecode = file.GetSpirV();
        _jfaShader = Glob.RD.ShaderCreateFromSpirV(bytecode);
      }
      _cellBuf = Glob.RD.StorageBufferCreate(CDimLen3 * sizeof(Int32));
      _noiseBuf = Glob.RD.StorageBufferCreate(NDimLen3 * sizeof(Single));
      _pUniforms.Add(new RDUniform
      { UniformType = RenderingDevice.UniformType.StorageBuffer, Binding = 0 });
      _pUniforms[0].AddId(_cellBuf);
      _pUniforms.Add(new RDUniform
      { UniformType = RenderingDevice.UniformType.StorageBuffer, Binding = 1 });
      _pUniforms[1].AddId(_noiseBuf);
      _uniformSet = Glob.RD.UniformSetCreate(_pUniforms, _jfaShader, 0);
      _pipeline = Glob.RD.ComputePipelineCreate(_jfaShader);
      _simplexNoise0 = new FastNoiseLite();
      _simplexNoise0.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
      _simplexNoise0.Seed = Glob.Save.Seed;
      _simplexNoise0.FractalGain = 0.3f;
      _simplexNoise0.FractalOctaves = 2;
      _simplexNoise0.Frequency = 0.01f;

      _simplexNoise1 = new FastNoiseLite();
      _simplexNoise1.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
      _simplexNoise1.Seed = Glob.Save.Seed + 1;
      _simplexNoise1.FractalGain = 0.3f;
      _simplexNoise1.FractalOctaves = 4;
      _simplexNoise1.Frequency = 0.01f;

      _valueNoise.Seed = Glob.Save.Seed;
      _valueNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Value;
      _valueNoise.Frequency = 1.0f;
      _cells = new Cell[CDimLen3];
      _noise = new Single[NDimLen3];
    }
    ~Gen() {
      Glob.RD.FreeRid(_uniformSet);
      Glob.RD.FreeRid(_pipeline);
      Glob.RD.FreeRid(_cellBuf);
      Glob.RD.FreeRid(_noiseBuf);
    }

    public Cell[] GenSdf(Vector3I skey) {
      _skey = skey;
      _pos = skey * Geometry.Size;
      for (Int32 i = 0; i < _cells.Length; ++i) {
        _cells[i].Dist = -128;
      }
      ComputeNoise();
      //return RunJfa();
      ComputeBoundry();
      //RunJfa();
      FillSdf();
      //var n = 0;
      for (Int32 i = 0; i < _cells.Length; ++i) {
        var v0 = Glob.Unflat(i, CDimLen);
        if (_cells[i].Dist == SByte.MinValue) {
          //n += 1;
          var s = Math.Sign(_noise[Glob.Flat(v0 + Vector3I.One, NDimLen)]);
          _cells[i].Dist = (SByte)(SByte.MaxValue * s);
        }
      }
      //if (n != 0) {
      //GD.PrintS("filled", n);
      //}
      for (Int32 i = 1; i < _prefabs.Length; ++i) {
        if (GenPrefab(i)) {
          break;
        }
      }
      return _cells;
    }
    Boolean GenPrefab(Int32 pfi) {
      Fb.Prefab prefab = _prefabs[pfi];
      var ruinSize = new Vector3I(prefab.Width, prefab.Height, prefab.Depth);
      var ruinDim = ruinSize / Geometry.Size + Vector3I.One * 2;
<<<<<<< HEAD
      var ruinKey = Glob.DivFloor(_skey, ruinDim);
=======
      var ruinKey = (Vector3I)((Vector3)_skey / ruinDim).Floor();
>>>>>>> 82d442f (first commit:)
      ruinKey.Y = 0;
      var rng = new System.Random(((ruinKey.Z & 255) << 16)
          | ((ruinKey.X & 255) << 8)
          | pfi);
      var n = rng.NextDouble();
      if (n < 0.9) {
        return false;
      }
      var begin0 = new Vector3I(
        rng.Next(4, Geometry.Size),
        0,
        rng.Next(4, Geometry.Size)
      );
      var worldY = (Int32)Math.Round(ComputeHeight(ruinKey * Geometry.Size + begin0));
      begin0.Y = Glob.Mod(worldY, Geometry.Size);
      var minY = worldY - 4;
      if (_pos.Y < minY - Geometry.Size) {
        return true;
      }
      var maxY = worldY + prefab.Height + 1;
      //GD.Print(_pos.Y, begin0);
      if (_pos.Y > maxY) {
        return true;
      }

      var ruinCell = Glob.Mod(_skey, ruinDim);
<<<<<<< HEAD
      ruinCell.Y = _skey.Y - Glob.DivFloor(worldY, Geometry.Size);
=======
      ruinCell.Y = _skey.Y - (Int32)MathF.Floor((Single)worldY / Geometry.Size);
>>>>>>> 82d442f (first commit:)
      //GD.PrintS(worldY, ruinCell.Y);

      var begin = (begin0 - ruinCell * Geometry.Size).Max(Vector3I.Zero);
      var end = (begin0 + ruinSize - ruinCell * Geometry.Size).Min(Vector3I.One * CDimLen);
      var lengths = end - begin;
      var pfBegin = (ruinCell * Geometry.Size - begin0).Max(Vector3I.Zero);

      for (Int32 i = 0; i < lengths.X; ++i) {
        for (Int32 j = 0; j < lengths.Y; ++j) {
          for (Int32 k = 0; k < lengths.Z; ++k) {
            var ci = Glob.Flat(new Vector3I(i, j, k) + begin, CDimLen);
            var pfci = Glob.Flat(new Vector3I(i, j, k) + pfBegin, ruinSize);
            var old = _cells[ci];
            if (old.Dist == -128 || (0 < old.Dist && old.Dist > ((Cell)prefab.Cells(pfci)).Dist)) {
              _cells[ci] = (Cell)prefab.Cells(pfci);
            }
          }
        }
      }
      return true;
    }

    Single ComputeHeight(Vector3I worldCell) {
      return _simplexNoise1.GetNoise2D(worldCell.X, worldCell.Z) * 32;
    }

    void ComputeNoise() {
      for (Int32 i = 0; i < _noise.Length; ++i) {
        var nv = Glob.Unflat(i, NDimLen);
        var worldCell = nv - Vector3I.One + _pos;
        var noise = 0.0f;//= -_simplexNoise0.GetNoise3Dv(worldCell) * 8;
        noise -= ComputeHeight(worldCell);
        noise += worldCell.Y;
        _noise[i] = noise;
      }
    }
    void ComputeBoundry() {
      for (Int32 i = 0; i < _cells.Length; ++i) {
        var v0 = Glob.Unflat(i, CDimLen);
        var nv0 = v0 + Vector3I.One;
        var ni0 = Glob.Flat(nv0, NDimLen);
        var surface = (Vector3)v0;
        var ratio = 0.0f;
        var n0 = _noise[ni0];
        var s0 = Math.Sign(n0);
        var norm = new Vector3(
            _noise[ni0 + NDimLen2] - _noise[ni0 - NDimLen2],
            _noise[ni0 + NDimLen] - _noise[ni0 - NDimLen],
            _noise[ni0 + 1] - _noise[ni0 - 1]).Normalized();
        if (s0 == 0) {
          _cells[i].Dist = (SByte)ratio;
          _cells[i].SetNormal(norm);
          continue;
        }
        var interceptCount = 0;
        var intercepts = Vector3.Inf;
        for (Int32 ax = 0; ax < 3; ++ax) {
          for (Int32 sign = -1; sign < 2; sign += 2) {
            var dir = Vector3.Zero;
            dir[ax] = sign;
            var n1 = _noise[Glob.Flat(nv0 + (Vector3I)dir, NDimLen)];
            var s1 = Math.Sign(n1);
            var intercept = sign * n0 / (n0 - n1);
            if ((norm * -s0).Dot(dir) > 0 && s0 != s1 && intercept > Single.Epsilon) {
              intercepts[ax] = intercept;
              interceptCount += 1;
            }
          }
        }
        if (0 == interceptCount) {
          continue;
        }
        var planeN = norm;
        var planeC = Vector3.Zero;
        var max = (Int32)intercepts.MinAxisIndex();
        planeC[max] = intercepts[max];
        if (interceptCount == 3) {
          planeN = new Vector3(intercepts.Y * intercepts.Z,
              intercepts.X * intercepts.Z,
              intercepts.X * intercepts.Y).Normalized();
        }
        ratio = planeN.Dot(planeC) / norm.Dot(planeN);
        surface = norm * ratio + v0;
        var d0 = _cells[i].Dist = SignDist(-ratio);
        _cells[i].SetNormal(norm);
        foreach (Vector3I neighbor in Neighbors) {
          var s2 = Math.Sign(_noise[Glob.Flat(nv0 + neighbor, NDimLen)]);
          var v2 = v0 + neighbor;
          if (InBounds(v2) && 0 != s2) {
            var dist = d0 + s2 * Glob.DistFac;
            var c2 = _cells[i];
            var diff = (norm * d0) + (neighbor * Glob.DistFac);
            var diffLen = diff.Length();
            var signDist = diffLen * s2;
            c2.Dist = (SByte)Math.Clamp(signDist, -127, 127);
            var i2 = Glob.Flat(v2, CDimLen);
            _fills.Enqueue(new Fill(i2, c2), diffLen);
          }
        }
      }
    }
    void AddNeighbors(Int32 i0) {
      var v0 = Glob.Unflat(i0, CDimLen);
      var c0 = _cells[i0];
      var norm = c0.GetNormal();
      var d0 = c0.Dist;
      var s0 = Math.Sign(d0);
      for (Int32 i = 0; i < Neighbors.Length; ++i) {
        var neighbor = Neighbors[i];
        var v1 = v0 + neighbor;
        var i1 = Glob.Flat(v1, CDimLen);
        if (!InBounds(v1) || -128 != _cells[i1].Dist) {
          continue;
        }

        var s1 = s0;

        if (Math.Abs(d0) <= Glob.DistFac) {
          var dp = norm.Dot(neighbor);
          var delta = MathF.Round(dp * Glob.DistFac);
          s1 = Math.Sign(delta);

          if (d0 == 0) {
            if (s1 == 0) {
              continue;
            }
          } else if (s1 != s0) {
            continue;
          }
        }
        //if (-s0 == s1) {
        //continue;
        //}
        var c1 = new Cell();
        c1.Id = c0.Id;
        var diff = (norm * d0) + (neighbor * Glob.DistFac);
        var diffLen = diff.Length();
        var signDist = diffLen * s1;
        c1.SetNormal(diff / signDist);
        c1.Dist = (SByte)Math.Clamp(signDist, -127, 127);
        _fills.Enqueue(new Fill(i1, c1), diffLen);
        //} else if (n != 0) {
        //Cell c1 = _cells[i0];
        //delta = s0 * Glob.DistFac;
        //c1.Dist = (SByte)Math.Clamp(d0 + delta, -127, 127);
        //_fills.Enqueue(new Fill(i1, c1), Math.Abs(c1.Dist));
        //}
      }
    }
    SByte SignDist(Single worldSignDist) {
      return (SByte)Math.Clamp(Math.Round(Glob.DistFac * worldSignDist), -127, 127);
    }
    void FillSdf() {
      while (_fills.TryDequeue(out Fill fill, out _)) {
        if (-128 != _cells[fill.Index].Dist) {
          continue;
        }
        _cells[fill.Index] = fill.cell;
        //AddNeighbors(fill.Index);
      }
    }
    private Boolean InBounds(Vector3I v) {
      return v.X >= 0 && v.Y >= 0 && v.Z >= 0
          && v.X < CDimLen && v.Y < CDimLen && v.Z < CDimLen;
    }
    Byte[] RunJfa() {
      var buf = ArrayPool<Byte>.Shared.Rent(NDimLen3 * sizeof(Int32));
      Buffer.BlockCopy(_cells, 0, buf, 0, CDimLen3 * sizeof(Int32));
      Glob.RD.BufferUpdate(_cellBuf, 0, CDimLen3 * sizeof(Int32), buf);
      Buffer.BlockCopy(_noise, 0, buf, 0, NDimLen3 * sizeof(Single));
      Glob.RD.BufferUpdate(_noiseBuf, 0, NDimLen3 * sizeof(Single), buf);
      ArrayPool<Byte>.Shared.Return(buf);
      var compute_list = Glob.RD.ComputeListBegin();
      Glob.RD.ComputeListBindComputePipeline(compute_list, _pipeline);
      Glob.RD.ComputeListBindUniformSet(compute_list, _uniformSet, 0);
      UInt32 groups = CDimLen / 8;
      Glob.RD.ComputeListDispatch(compute_list, groups, groups, groups);
      Glob.RD.ComputeListEnd();
      Glob.RD.Submit();
      Glob.RD.Sync();
      return Glob.RD.BufferGetData(_cellBuf);
    }
  }
}
