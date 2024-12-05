using System;
using Godot;
<<<<<<< HEAD
using System.Collections.Generic;
using System.Buffers;
namespace Chunk {
  public class Compute {
    static Rid[] _shaders;
    static Rid[] _pipelines;
    static void Init() {
      _shaders = new Rid[(Int32)AoeShape.Count];
      _pipelines = new Rid[(Int32)AoeShape.Count];
      for (Int32 i = 0; i < (Int32)AoeShape.Count; ++i) {
        var shaderFile = ((AoeShape)i) switch
        {
          AoeShape.Cube => GD.Load<RDShaderFile>("res://shaders/apply_cube.glsl"),
          AoeShape.Sphere => GD.Load<RDShaderFile>("res://shaders/apply_sphere.glsl"),
          AoeShape.Cylinder => GD.Load<RDShaderFile>("res://shaders/apply_cylinder.glsl"),
        };
        var bytecode = shaderFile.GetSpirV();
        _shaders[i] = Glob.RD.ShaderCreateFromSpirV(bytecode);
        _pipelines[i] = Glob.RD.ComputePipelineCreate(_shaders[i]);
      }
    }
    static void Dispose() {
      for (Int32 i = 0; i < (Int32)AoeShape.Count; ++i) {
        Glob.RD.FreeRid(_shaders[i]);
        Glob.RD.FreeRid(_pipelines[i]);
      }
    }
    static public void ApplySdf(List<Save> saves, Aoe aoe, Transform3D tsf, Int16 blockId) {
      if (_pipelines == null) {
        Init();
      }
      var shapei = (Int32)aoe.AoeShape;
      List<Rid> uniformSets = new();
      for (Int32 i = 0; i < saves.Count; ++i) {
        var pUniforms = saves[i].GetPUniforms(tsf, blockId);
        uniformSets.Add(Glob.RD.UniformSetCreate(pUniforms, _shaders[shapei], 0));
      }
      UInt32 saveGroups = Geometry.DimLen / 8;
      var computeList = Glob.RD.ComputeListBegin();
      Glob.RD.ComputeListBindComputePipeline(computeList, _pipelines[shapei]);
      for (Int32 i = 0; i < uniformSets.Count; ++i) {
        Glob.RD.ComputeListBindUniformSet(computeList, uniformSets[i], 0);
        Glob.RD.ComputeListDispatch(computeList, saveGroups, saveGroups, saveGroups);
      }
      Glob.RD.ComputeListAddBarrier(computeList);
      Glob.RD.ComputeListEnd(RenderingDevice.BarrierMask.AllBarriers);
      Glob.RD.Submit();
      Glob.RD.Sync();
      for (Int32 i = 0; i < saves.Count; ++i) {
        saves[i].ComputeUpdate();
        Glob.RD.FreeRid(uniformSets[i]);
      }
      //Glob.RD.BufferUpdate(_cellBuf, 0, Geometry.DimLen3 * sizeof(Int32), res);
    }

    Rid _cellBuf;
    Rid _uniformBuf;
    public Godot.Collections.Array<RDUniform> PUniforms = new();
    public Compute() {
      _cellBuf = Glob.RD.StorageBufferCreate(Geometry.DimLen3 * sizeof(Int32));
      PUniforms.Add(new RDUniform
      { UniformType = RenderingDevice.UniformType.StorageBuffer, Binding = 0 });
      PUniforms[0].AddId(_cellBuf);
      _uniformBuf = Glob.RD.UniformBufferCreate((16 + 12 + 4 + 4) * sizeof(Int32));
      PUniforms.Add(new RDUniform
      { UniformType = RenderingDevice.UniformType.UniformBuffer, Binding = 1 });
      PUniforms[1].AddId(_uniformBuf);
    }
    public void UpdateUniformBuf(Transform3D tsf, Vector3 pos, Int32 blockId) {
      var bytes = (16 + 12 + 4 + 4) * sizeof(Single);
      var uBuf = ArrayPool<Byte>.Shared.Rent(bytes);
      var arr = new Single[16 + 12 + 4];
      var inv_tsf = tsf.AffineInverse();
      for (Int32 i = 0; i < 4; ++i) {
        for (Int32 j = 0; j < 3; ++j) {
          arr[i * 4 + j] = inv_tsf[i][j];
        }
      }
      arr[3] = 0.0f;
      arr[7] = 0.0f;
      arr[11] = 0.0f;
      arr[15] = 1.0f;
      for (Int32 i = 0; i < 3; ++i) {
        for (Int32 j = 0; j < 3; ++j) {
          arr[16 + i * 4 + j] = tsf[i][j];
        }
      }
      arr[19] = 0.0f;
      arr[23] = 0.0f;
      arr[27] = 0.0f;
      arr[28] = pos.X;
      arr[29] = pos.Y;
      arr[30] = pos.Z;
      arr[31] = 1.0f;
      var arrByteCount = arr.Length * sizeof(Single);
      Buffer.BlockCopy(arr, 0, uBuf, 0, arrByteCount);
      var idBytes = BitConverter.GetBytes(blockId);
      Buffer.BlockCopy(idBytes, 0, uBuf, arrByteCount, sizeof(Int32));
      Glob.RD.BufferUpdate(_uniformBuf, 0, (UInt32)bytes, uBuf);
      ArrayPool<Byte>.Shared.Return(uBuf);
=======
using System.Buffers;
namespace Chunk {
  class Compute {
    static Rid[] _shaders;
    static Rid[] _pipelines;
    Rid[] _uniformSets;
    Rid _cellBuf;
    Rid _uniformBuf;
    Godot.Collections.Array<RDUniform> _pUniforms = new();
    static void Init() {
      _shaders = new Rid[(Int32)AoeShape.Count];
      _pipelines = new Rid[(Int32)AoeShape.Count];
      for (Int32 i = 0; i < 1; ++i) {
        if (!_shaders[i].IsValid) {
          GD.Print($"compiling shader {i}");
          var shaderFile = ((AoeShape)i) switch
          {
            AoeShape.Cube => GD.Load<RDShaderFile>("res://shaders/apply_sphere.glsl"),
            AoeShape.Sphere => GD.Load<RDShaderFile>("res://shaders/apply_sphere.glsl"),
            AoeShape.Cylinder => GD.Load<RDShaderFile>("res://shaders/apply_cylinder.glsl"),
          };
          var bytecode = shaderFile.GetSpirV();
          _shaders[i] = Glob.RD.ShaderCreateFromSpirV(bytecode);
        }
        if (!_pipelines[i].IsValid) {
          GD.Print($"creating pipeline {i}");
          _pipelines[i] = Glob.RD.ComputePipelineCreate(_shaders[i]);
        }
      }
    }
    public Compute() {
      if (_pipelines == null) {
        Init();
      }
      _cellBuf = Glob.RD.StorageBufferCreate(Geometry.DimLen3 * sizeof(Int32));
      _pUniforms.Add(new RDUniform
      { UniformType = RenderingDevice.UniformType.StorageBuffer, Binding = 0 });
      _pUniforms[0].AddId(_cellBuf);
      _uniformBuf = Glob.RD.UniformBufferCreate((4 + 16 + 4) * sizeof(Int32));
      _pUniforms.Add(new RDUniform
      { UniformType = RenderingDevice.UniformType.UniformBuffer, Binding = 1 });
      _pUniforms[1].AddId(_uniformBuf);
      _uniformSets = new Rid[(Int32)AoeShape.Count];
      for (Int32 i = 0; i < 1; ++i) {
        _uniformSets[i] = Glob.RD.UniformSetCreate(_pUniforms, _shaders[i], 0);
      }
    }
    ~Compute() {
      Glob.RD.FreeRid(_cellBuf);
      Glob.RD.FreeRid(_uniformBuf);
      for (Int32 i = 0; i < (Int32)AoeShape.Count; ++i) {
        if (_shaders[i].IsValid) {
          Glob.RD.FreeRid(_shaders[i]);
          _shaders[i] = new Rid();
        }
        Glob.RD.FreeRid(_uniformSets[i]);
        if (_pipelines[i].IsValid) {
          Glob.RD.FreeRid(_pipelines[i]);
          _shaders[i] = new Rid();
        }
      }
>>>>>>> 82d442f (first commit:)
    }
    public void UpdateCellBuf(Int32[] cells) {
      var bytes = Geometry.DimLen3 * sizeof(Int32);
      var buf = ArrayPool<Byte>.Shared.Rent(bytes);
      Buffer.BlockCopy(cells, 0, buf, 0, bytes);
      Glob.RD.BufferUpdate(_cellBuf, 0, (UInt32)bytes, buf);
      ArrayPool<Byte>.Shared.Return(buf);
    }
<<<<<<< HEAD
    public Byte[] GetCellBuffer() {
      return Glob.RD.BufferGetData(_cellBuf);
    }
    ~Compute() {
      Glob.RD.FreeRid(_cellBuf);
      Glob.RD.FreeRid(_uniformBuf);
    }

=======
    void UpdateUniformBuf(Transform3D tsf, Vector3 pos, Int32 blockId) {
      var bytes = (16 + 4 + 1) * sizeof(Single);
      var uBuf = ArrayPool<Byte>.Shared.Rent(bytes);
      var arr = new Single[16 + 4];
      for (Int32 i = 0; i < 4; ++i) {
        for (Int32 j = 0; j < 3; ++j) {
          arr[i * 4 + j] = tsf[i][j];
        }
      }
      arr[3] = 0.0f;
      arr[7] = 0.0f;
      arr[11] = 0.0f;
      arr[15] = 1.0f;
      arr[16] = pos.X;
      arr[17] = pos.Y;
      arr[18] = pos.Z;
      arr[19] = 1.0f;

      Buffer.BlockCopy(arr, 0, uBuf, 0, 64);
      GD.Print("building unifomrs");
      Buffer.BlockCopy(BitConverter.GetBytes(blockId), 0, uBuf, (16 + 4) * sizeof(Int32), 4);
      Glob.RD.BufferUpdate(_uniformBuf, 0, (UInt32)bytes, uBuf);
      ArrayPool<Byte>.Shared.Return(uBuf);
    }
    public Byte[] ApplySdf(Aoe aoe, Transform3D tsf, Vector3 pos, Int16 blockId) {
      UpdateUniformBuf(tsf, pos, blockId);
      var id = (Int32)aoe.AoeShape;
      var shader = _shaders[id];
      var computeList = Glob.RD.ComputeListBegin();
      Glob.RD.ComputeListBindComputePipeline(computeList, _pipelines[id]);
      Glob.RD.ComputeListBindUniformSet(computeList, _uniformSets[id], 0);
      UInt32 saveGroups = Geometry.DimLen / 8;
      Glob.RD.ComputeListDispatch(computeList, saveGroups, saveGroups, saveGroups);
      Glob.RD.ComputeListEnd();
      Glob.RD.Submit();
      Glob.RD.Sync();
      return Glob.RD.BufferGetData(_cellBuf);
    }
>>>>>>> 82d442f (first commit:)
  }
}
