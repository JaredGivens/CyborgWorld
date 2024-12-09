using Godot;
using System;
using System.Collections.Generic;
namespace Player {
  public partial class Controller : CharacterBody3D {
    [Export]
    public Single Speed = 10.0f;
    [Export]
    private Single _sens = 0.01f;
    [Export]
    private Single _hover = 100.0f;
    private Camera3D _cam;


    // Get the gravity from the project settings to be synced with RigidBody nodes.
    private Cursor _cursor;
    public PauseMenu _pauseMenu;
    private Hotbar _hotbar;
    private Save _save;
    public string Uuid = "0";
    private Inventory _inventory;
    private Control _currentUI;
    private Boolean _stored;
    private AnimationPlayer _animPlayer;
    private Rid _space;
    public Controller() : base() {
      _save = new Save(Uuid);
      Transform = _save.PlayerTransform;
      Velocity = _save.Velocity;
    }
    public override void _Ready() {
      _pauseMenu = GetNode<PauseMenu>("PauseMenu");
      _pauseMenu.Init(GetParent<Game>());
      _cam = GetNode<Camera3D>("Camera3D");
      _hotbar = GetNode<Hotbar>("Hotbar");
      _cursor = GetNode<Cursor>("Cursor");
      _inventory = GetNode<Inventory>("Inventory");
      _animPlayer = GetNode<AnimationPlayer>("cyborg/AnimationPlayer");
      _animPlayer.PlaybackDefaultBlendTime = 0.5;
      _cam.Basis = _save.CameraTransform.Basis;
      _pauseMenu.SetProcessInput(false);
      _inventory.SetProcessInput(false);
      _hotbar.SetProcessInput(false);
      _space = GetWorld3D().Space;
      _hotbar.BindStacks(_save.HotbarStacks);

      Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    private Boolean CloseUI() {
      if (_currentUI == null) {
        return false;
      }
      _currentUI.SetProcessInput(false);
      _currentUI.Visible = false;
      _currentUI = null;
      _hotbar.UpdateStacks();
      return true;
    }
    private void ShowControl(Control control) {
      if (!CloseUI()) {
        Input.MouseMode = Input.MouseModeEnum.Visible;
      }
      control.Visible = true;
      control.SetProcessInput(true);
      _currentUI = control;
    }

    public override void _UnhandledInput(InputEvent @event) {
      if (_currentUI == null) {
        if (@event is InputEventMouseMotion mouseMotion) {
          RotateY(mouseMotion.Relative.X * -_sens);
          var xRot = mouseMotion.Relative.Y * -_sens;
          _cam.Rotation = new Vector3(
          Math.Clamp(_cam.Rotation.X + xRot, -MathF.PI / 2, MathF.PI / 2),
          0, 0);
        }
      }
      if (@event.IsActionPressed("ui_cancel")) {
        if (CloseUI()) {
          Input.MouseMode = Input.MouseModeEnum.Captured;
          return;
        }
        GD.Print(Position);
        ShowControl(_pauseMenu);
      }
      for (Int32 i = 0; i < 4; ++i) {
        if (@event.IsActionPressed($"slot{i}")) {
          _save.HotbarSelection = i;
          return;
        }
      }
      if (@event.IsActionPressed("inventory")) {
        _inventory.Populate(_save);
        ShowControl(_inventory);
        return;
      }
      if (@event.IsActionPressed("interact")) {
        GD.Print("here");
        var facing = -_cam.GlobalBasis.Z.Normalized();
        var origin = _cam.GlobalPosition;
        var ray = PhysicsRayQueryParameters3D
          .Create(origin, facing * 8 + origin);
        var result = PhysicsServer3D.SpaceGetDirectState(_space)
          .IntersectRay(ray);
        if (!result.ContainsKey("position")) {
          return;
        }
        var pos = (Vector3)result["position"];
        var res = GetParent<Game>().Terrain.Interact(pos);
        if (res is (Chunk.BlockId id, Memory<Int16> items)) {
          GD.Print(id);
        }
        //ToggleUI(inv

        return;
      }
      if (@event.IsActionPressed("ui_focus_next")) {
        // tab
        return;
      }
      if (@event.IsActionPressed("use_unit")) {
        var stack = _hotbar.Units[_save.HotbarSelection].Stack;
        if (stack.Amt == 0) {
          return;
        }
        if (stack.Id > Glob.Units.Count) {
          // TODO spell
          return;
        }
        switch (Glob.Units[stack.Id].Type) {
          case UnitType.Terraform:
          if (!_cursor.Visible) {
            break;
          }
          using (var cube = Aoe.GetAoe(AoeShape.Sphere)) {
            var tsf = _cursor.GlobalTransform.ScaledLocal(Vector3.One * 8);
            GetParent<Game>().Terrain.ApplySdf(cube, tsf, (Int16)Chunk.BlockId.Scanner);
          }
          break;
        }
        return;
      }
      if (@event.IsActionPressed("wireframe")) {
        GetViewport().DebugDraw =
          GetViewport().DebugDraw == Viewport.DebugDrawEnum.Disabled
          ? Viewport.DebugDrawEnum.Wireframe
          : Viewport.DebugDrawEnum.Disabled;
        return;
      }
      if (@event.IsActionPressed("colors")) {
        Chunk.Geometry.Options ^= Chunk.DisplayOptions.Colors;
        return;
      }
      if (@event.IsActionPressed("normals")) {
        Chunk.Geometry.Options ^= Chunk.DisplayOptions.Normals;
        return;
      }
      if (@event.IsActionPressed("boundries")) {
        Chunk.Geometry.Options ^= Chunk.DisplayOptions.Boundries;
        return;
      }
    }
    private void UpdateCursor() {
      var facing = -_cam.GlobalBasis.Z.Normalized();
      var origin = _cam.GlobalPosition;
      var stack = _hotbar.Units[_save.HotbarSelection].Stack;
      _cursor.Update(origin, facing, stack);

    }
    private Single _gravity = ProjectSettings
      .GetSetting("physics/3d/default_gravity").AsSingle();
    public override void _PhysicsProcess(double delta) {
      if (_stored) {
        return;
      }
      Vector3 velocity = Velocity;
      velocity.Y -= _gravity * (Single)delta;

      // Get the input direction and handle the movement/deceleration.
      // As good practice, you should replace UI actions with custom gameplay actions.
      if (Input.IsActionPressed("jump")) {
        if (Glob.Save.Gamemode == GamemodeEnum.Sandbox || IsOnFloor()) {
          velocity.Y = _hover * (Single)delta;
          _animPlayer.Play("jump");
        }
      }
      Vector2 inputDir = Input.GetVector("left", "right", "forward", "back");
      Vector3 direction = (Transform.Basis *
          new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
      if (direction != Vector3.Zero) {
        velocity.X = direction.X * Speed;
        velocity.Z = direction.Z * Speed;
      } else {
        velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
        velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
      }
      if (Math.Abs(inputDir.X) > Math.Abs(inputDir.Y)) {
        if (inputDir.X > 0) {
          _animPlayer.Play("right");
        } else {
          _animPlayer.Play("left");
        }
      } else if (Math.Abs(inputDir.Y) > Math.Abs(inputDir.X)) {
        if (inputDir.Y > 0) {
          _animPlayer.Play("back");
        } else {
          _animPlayer.Play("forward");
        }
      } else {
        _animPlayer.Play("idle");
      }

      Velocity = velocity;
      MoveAndSlide();
      UpdateCursor();
    }
    public void Store() {
      _save.PlayerTransform = Transform;
      _save.CameraTransform = _cam.Transform;
      _save.Velocity = Velocity;
      _save.Store();
      _stored = true;
    }
  }
}
