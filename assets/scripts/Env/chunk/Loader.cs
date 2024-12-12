using System;
using Godot;
using System.Collections.Generic;
namespace Chunk {
  static class Loader {
    private static Int32 _texSize;
    private static Int32 _atlasWidth;
    private static Int32 _atlasHeight;
    private static Godot.Collections.Dictionary _albedoJson;
    public static ShaderMaterial TerrainShaderMat;
    public static Mesh GrassMesh;
    public static ShaderMaterial GrassShaderMat;
    private static Node _grassSceneInst;
    public static Vector2[] BlockUvs = new Vector2[(Int32)BlockId.Count];
    public static Dictionary<BlockId, Control> BlockUis = new();
    static Int32 frameId(Godot.Collections.Dictionary atlas, String filename) {
      var frames = (Godot.Collections.Dictionary)atlas["frames"];
      var tex = (Godot.Collections.Dictionary)frames[filename];
      var frame = (Godot.Collections.Dictionary)tex["frame"];
      var x = (Int32)frame["x"];
      var y = (Int32)frame["y"];
      return x / _texSize + y / _texSize * _atlasWidth / _texSize;
    }
    static void InitBlocks() {
      var fns = DirAccess.GetFilesAt("res://resources/blocks");
      foreach (var fn in fns) {
        var block = GD.Load<Block>($"res://resources/blocks/{fn}");
        var fid = frameId(_albedoJson, block.AlbedoFn);
        var uv = new Vector2(fid, (Single)block.Mapping);
        BlockUvs[(Int32)block.BlockId] = uv;
        if (block.Ui is PackedScene scene) {
          var node = scene.Instantiate<Control>();
          node.Visible = false;
          BlockUis.Add(block.BlockId, node);
        }
      }
    }
    static void InitTerrainShader() {
      TerrainShaderMat = new();
      TerrainShaderMat.Shader = GD.Load<Shader>("res://shaders/chunk.gdshader");
      TerrainShaderMat.SetShaderParameter("chunk_size", Geometry.DimLen);
      var albedoAtlas = GD
        .Load<Texture2D>("res://textures/chunk/albedo_atlas.png");
      var ormAtlas = GD.Load<Texture2D>("res://textures/chunk/orm_atlas.png");
      var nhAtlas = GD.Load<Texture2D>("res://textures/chunk/nh_atlas.png");
      TerrainShaderMat.SetShaderParameter("albedo_atlas", albedoAtlas);
      TerrainShaderMat.SetShaderParameter("orm_atlas", ormAtlas);
      TerrainShaderMat.SetShaderParameter("nh_atlas", nhAtlas);
      var atlasH = _atlasHeight / _texSize;
      var atlasW = _atlasWidth / _texSize;
      TerrainShaderMat.SetShaderParameter("atlas_height", atlasH);
      TerrainShaderMat.SetShaderParameter("atlas_width", atlasW);
    }
    public static void Init() {
      if (TerrainShaderMat != null) {
        return;
      }
      var json = new Json();
      var res = json.Parse(FileAccess
          .GetFileAsString("res://textures/chunk/albedo_atlas.json"));
      if (res != Error.Ok) {
        GD.PrintErr("Atlas json parse error");
        return;
      }
      _albedoJson = (Godot.Collections.Dictionary)json.Data;
      {
        var meta = (Godot.Collections.Dictionary)_albedoJson["meta"];
        var size = (Godot.Collections.Dictionary)meta["size"];
        _atlasHeight = (Int32)size["h"];
        _atlasWidth = (Int32)size["w"];
        GD.PrintS(BlockId.Grass, BlockId.Dirt);
      }
      {
        var frames = (Godot.Collections.Dictionary)_albedoJson["frames"];
        var grass = (Godot.Collections.Dictionary)frames
          ["mossy-ground1-albedo.png"];
        var size = (Godot.Collections.Dictionary)grass["sourceSize"];
        _texSize = (Int32)size["h"];
      }
      InitTerrainShader();
      InitBlocks();
    }

    public static void InitGrass(Mesh grass) {
      GrassShaderMat = GD.Load<ShaderMaterial>("res://resources/grass.tres");
      GrassMesh = grass;
    }
    public static void SetGrassPlayer(Vector3 pos) {
      GrassShaderMat.SetShaderParameter("character_position", pos);
    }
  }
}
