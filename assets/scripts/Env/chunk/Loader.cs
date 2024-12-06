using System;
using Godot;
namespace Chunk {
  static class Loader {
    private static Int32 _texSize;
    private static Int32 _atlasWidth;
    private static Int32 _atlasHeight;
    private static Godot.Collections.Dictionary _albedoJson;
    public static ShaderMaterial ShaderMat;
    public static Vector2[] BlockUvs = new Vector2[(Int32)BlockId.Count];
    static Int32 frameId(Godot.Collections.Dictionary atlas, string filename) {
      var frames = (Godot.Collections.Dictionary)atlas["frames"];
      var tex = (Godot.Collections.Dictionary)frames[filename];
      var frame = (Godot.Collections.Dictionary)tex["frame"];
      var x = (Int32)frame["x"];
      var y = (Int32)frame["y"];
      return x / _texSize + y / _texSize * _atlasWidth / _texSize;
    }
    static void InitBlockUvs() {
      BlockUvs[(Int32)BlockId.Grass] = new Vector2(
          frameId(_albedoJson, "grassy-meadow1_albedo.png"), (Single)BlockMapping.Uniform);
      BlockUvs[(Int32)BlockId.Dirt] = new Vector2(
          frameId(_albedoJson, "dirtwithrocks_Base_Color.png"), (Single)BlockMapping.Uniform);
      BlockUvs[(Int32)BlockId.Stone] = new Vector2(
          frameId(_albedoJson, "cement_arcing_pattern1_albedo.png"), (Single)BlockMapping.Uniform);
      BlockUvs[(Int32)BlockId.Scanner] = new Vector2(
          frameId(_albedoJson, "old-console-monitor_albedo.png"), (Single)BlockMapping.Uniform);
      BlockUvs[(Int32)BlockId.Chest] = new Vector2(
          frameId(_albedoJson, "aluminum-squares_albedo.png"), (Single)BlockMapping.Uniform);
      BlockUvs[(Int32)BlockId.Cement] = new Vector2(
          frameId(_albedoJson, "cement_arcing_pattern1_albedo.png"), (Single)BlockMapping.Uniform);

    }
    static void InitShader() {
      ShaderMat = new();
      ShaderMat.Shader = GD.Load<Shader>("res://shaders/chunk.gdshader");
      ShaderMat.SetShaderParameter("chunk_size", Geometry.DimLen);
      var albedoAtlas = GD.Load<Texture2D>("res://textures/chunk/albedo_atlas.png");
      var ormAtlas = GD.Load<Texture2D>("res://textures/chunk/orm_atlas.png");
      var nhAtlas = GD.Load<Texture2D>("res://textures/chunk/nh_atlas.png");
      ShaderMat.SetShaderParameter("albedo_atlas", albedoAtlas);
      ShaderMat.SetShaderParameter("orm_atlas", ormAtlas);
      ShaderMat.SetShaderParameter("nh_atlas", nhAtlas);
      ShaderMat.SetShaderParameter("atlas_height", _atlasHeight / _texSize);
      ShaderMat.SetShaderParameter("atlas_width", _atlasWidth / _texSize);
    }
    public static void Init() {
      if (ShaderMat != null) {
        return;
      }
      var json = new Json();
      var res = json.Parse(FileAccess.GetFileAsString("res://textures/chunk/albedo_atlas.json"));
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
        var grass = (Godot.Collections.Dictionary)frames["grassy-meadow1_albedo.png"];
        var size = (Godot.Collections.Dictionary)grass["sourceSize"];
        _texSize = (Int32)size["h"];
      }
      InitShader();
      InitBlockUvs();
    }
  }
}