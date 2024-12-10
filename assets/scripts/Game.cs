using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class Game : Node3D {
  private enum State {
    SaveMenu,
    LoadingGame,
    Game,
    SavingGame,
    QuittingGame,
  }
  private readonly PackedScene _playerScene = GD.Load<PackedScene>("res://scenes/player.tscn");
  private Player.Controller _player;
  [Export]
  private Loading _loadingScreen;
  [Export]
  private SaveMenu _saveMenu;
  [Export]
  private MeshInstance3D grass;
  private State _state = State.SaveMenu;
  public Terrain Terrain;
  Game() {
    LoadUnits();
    Aoe.InitAoes();
  }
  ~Game() {
    Chunk.Compute.Dispose();
  }
  public override void _Ready() {
    Chunk.Loader.Init();
    Chunk.Loader.InitGrass(grass.Mesh);
  }
  private void LoadUnits() {
    Glob.Units = new List<Unit>();
    var fns = DirAccess.GetFilesAt("res://resources/units");
    foreach (var fn in fns) {
      GD.Print($"res://resources/units/{fn}");
      Glob.Units.Add(GD.Load<Unit>($"res://resources/units/{fn}"));
    }
  }
  public override void _Process(Double delta) {
    switch (_state) {
      case State.SaveMenu:
      if (Terrain == null) {
        Terrain = new(GetWorld3D().Scenario, GetWorld3D().Space);
      }
      foreach (var container in _saveMenu.SaveContainers) {
        if (container.Pressed) {
          container.Pressed = false;
          Glob.Save = container._save;
          Glob.SavePath = container._path;
          _saveMenu.Visible = false;
          _saveMenu.SetProcessInput(false);
          _loadingScreen.StartLoading(
              () => (Single)Terrain.LoadedSaves / MathF.Pow(Glob.LoadDist, 3));
          _player = _playerScene.Instantiate<Player.Controller>();
          Terrain.Load(_player.Position);
          _state = State.LoadingGame;
        }
      }
      break;
      case State.LoadingGame:
      if (_loadingScreen.IsFinished()) {
        AddChild(_player);
        _state = State.Game;
      }
      break;
      case State.Game:
      Terrain.Process(_player.Position);
      break;
      case State.SavingGame:
      if (_loadingScreen.IsFinished()) {
        _saveMenu.Visible = true;
        _saveMenu.SetProcessInput(true);
        _state = State.SaveMenu;
        Terrain = null;
      }
      break;
      case State.QuittingGame:
      if (_loadingScreen.IsFinished()) {
        CallDeferred(nameof(Quit));
      }
      break;
    }
  }
  public override void _Notification(Int32 what) {
    switch ((Int64)what) {
      case NotificationWMCloseRequest:
      StoreExit();
      break;
    }
  }
  private void Quit() => GetTree().Quit();
  private void Store() {
    _player.Store();
    var totalTasks = Terrain.RunningTaskCount;
    if (0 != totalTasks) {
      _loadingScreen.StartLoading(() => ((Single)totalTasks - Terrain.RunningTaskCount) / totalTasks);
    } else {
      _loadingScreen.StartLoading(() => 1.0f);
    }
    Terrain.Dispose();
    RemoveChild(_player);
  }
  public void StoreExit() {
    GD.Print("Saving ...");
    Store();
    CallDeferred(nameof(Quit));
    _state = State.QuittingGame;
  }

  public void StoreHome() {
    GD.Print("Saving ...");
    Store();
    _state = State.SavingGame;
  }

}
