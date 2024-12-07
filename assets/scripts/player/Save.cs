using System;
using Godot;
using System.Linq;
using System.IO;
using Google.FlatBuffers;
namespace Player {
  public class Save {
    public FlatBufferBuilder _builder = new(4028);
    public Transform3D PlayerTransform;
    public Transform3D CameraTransform;
    public Vector3 Velocity = new();
    public Int32 Level = 1;
    public Int32 Health = 100;
    public Int32 MaxHealth = 100;
    public Int16[] Phases;
    public Int16[] InventoryStacks;
    public Int16[] HotbarStacks;
    public String Uuid;
    private String _path;
    public Int32 HotbarSelection;
    public Save(string uuid) {
      Uuid = uuid;
      _path = $"{Glob.SavePath}/player_data/p.{Uuid}.tres";
      Phases = new Int16[64];
      InventoryStacks = new Int16[64];
      HotbarStacks = new Int16[4];
      PlayerTransform = new Transform3D(Basis.Identity, Glob.Save.Spawn);
      CameraTransform = Transform3D.Identity;
      Load();
    }
    Transform3D LoadTransform(Fb.Tsf3? entry) {
      if (entry is Fb.Tsf3 tsf) {
        return new(
            LoadVec(tsf.Column0),
            LoadVec(tsf.Column1),
            LoadVec(tsf.Column2),
            LoadVec(tsf.Origin));
      }
      return Transform3D.Identity;
    }
    public void Load() {
      if (!File.Exists(_path)) {
        return;
      }
      try {
        Fb.Player player;
        var bytes = File.ReadAllBytes(_path);
        player = Fb.Player.GetRootAsPlayer(new ByteBuffer(bytes));
        HotbarSelection = player.HotbarSelection;
        PlayerTransform = LoadTransform(player.PlayerTransform);
        CameraTransform = LoadTransform(player.CameraTransform);
        Velocity = LoadVec(player.Velocity);
        Phases = player.GetPhasesArray();
        InventoryStacks = player.GetInventoryArray();
        HotbarStacks = player.GetHotbarArray();
      } catch (Exception e) {
        GD.PrintErr($"Failed to load player save: {e}");
      }
    }
    Offset<Fb.Vec3> StoreVec(Vector3 src) {
      return Fb.Vec3.CreateVec3(_builder, src.X, src.Y, src.Z);
    }
    Vector3 LoadVec(Fb.Vec3? entry) {
      if (entry is Fb.Vec3 vec) {
        return new Vector3(vec.X, vec.Y, vec.Z);
      }
      return Vector3.Zero;
    }
    public Offset<Fb.Tsf3> StoreTsf(Transform3D src) {
      return Fb.Tsf3.CreateTsf3(_builder,
          src[0][0], src[0][1], src[0][2],
          src[1][0], src[1][1], src[1][2],
          src[2][0], src[2][1], src[2][2],
          src[3][0], src[3][1], src[3][2]);
    }

    void LoadTsf(Fb.Vec3 src, ref Vector3 dest) {
      dest.X = src.X;
      dest.Y = src.Y;
      dest.Z = src.Z;
    }
    void LoadVec(Fb.Vec3 src, ref Vector3 dest) {
      dest.X = src.X;
      dest.Y = src.Y;
      dest.Z = src.Z;
    }
    public void Store() {
      _builder.Clear();
      var phases = Fb.Player.CreatePhasesVector(_builder, Phases);
      var inv = Fb.Player.CreateInventoryVector(_builder, InventoryStacks);
      var hotbar = Fb.Player.CreateHotbarVector(_builder, HotbarStacks);
      Fb.Player.StartPlayer(_builder);
      Fb.Player.AddPlayerTransform(_builder, StoreTsf(PlayerTransform));
      Fb.Player.AddCameraTransform(_builder, StoreTsf(CameraTransform));
      Fb.Player.AddVelocity(_builder, StoreVec(Velocity));
      Fb.Player.AddHotbarSelection(_builder, HotbarSelection);
      Fb.Player.AddLevel(_builder, Level);
      Fb.Player.AddHealth(_builder, Health);
      Fb.Player.AddMaxHealth(_builder, MaxHealth);
      Fb.Player.AddPhases(_builder, phases);
      Fb.Player.AddInventory(_builder, inv);
      Fb.Player.AddHotbar(_builder, hotbar);
      var player = Fb.Player.EndPlayer(_builder);
      _builder.Finish(player.Value);
      var arr = _builder.SizedByteArray();
      using (var fs = File
          .Open(_path, FileMode.Create, System.IO.FileAccess.Write)) {
        fs.Write(arr, 0, arr.Length);
      }
    }
  }
}
