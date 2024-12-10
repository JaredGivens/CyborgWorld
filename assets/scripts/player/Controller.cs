using Godot;
using System;
using System.Collections.Generic;
namespace Player {
  public partial class Controller : CharacterBody3D {
    [Export]
    public Single Speed = 10.0f;
    [Export]
    public Single TermVel = 100.0f;
    [Export]
    public Single Friction = 0.8f;
    [Export]
    public Single Acceleration = 1.0f;
    [Export]
    private Single _sens = 0.01f;
    [Export]
    private Single _hover = 100.0f;
    [Export]
    private Camera3D _cam;
    [Export]
    public PauseMenu _pauseMenu;
    [Export]
    private Cursor _cursor;
    [Export]
    private Hotbar _hotbar;
    [Export]
    private Inventory _inventory;
    [Export]
    private AnimationTree _animTree;

    private Save _save;
    public string Uuid = "0";
    private Control _currentUI;
    private Boolean _stored;
    private Rid _space;
    public Controller() : base() {
      _save = new Save(Uuid);
      Transform = _save.PlayerTransform;
      Velocity = _save.Velocity;
    }
    public override void _Ready() {
      _pauseMenu.Init(GetParent<Game>());
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
        var unit = Glob.Units[stack.Id];
        switch (unit.Type) {
          case UnitType.Terraform:
          if (!_cursor.Visible) {
            break;
          }
          using (var shape = Aoe.GetAoe(unit.Shape)) {
            var tsf = _cursor.GlobalTransform;
            GetParent<Game>().Terrain.ApplySdf(shape, tsf, (Int16)Chunk.BlockId.None);
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
    public override void _PhysicsProcess(Double delta) {
      if (_stored) {
        return;
      }
      Vector3 velocity = Velocity;
      velocity.Y -= _gravity * (Single)delta;

      // Get the input direction and handle the movement/deceleration.
      // As good practice, you should replace UI actions with custom gameplay actions.
      Vector2 inputDir = Input.GetVector("left", "right", "forward", "back");
      Vector3 direction = Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y);
      if (direction != Vector3.Zero) {
        velocity += (direction * Acceleration);
      } else if (IsOnFloor()) {
        velocity.X = Mathf.Lerp(velocity.X, 0.0f, Friction);
        velocity.Z = Mathf.Lerp(velocity.Z, 0.0f, Friction);
      }
      if (Input.IsActionPressed("jump")) {
        if (Glob.Save.Gamemode == GamemodeEnum.Sandbox || IsOnFloor()) {
          velocity.Y = _hover * (Single)delta;
          _animTree.Set("parameters/Jump/request", (Int32)AnimationNodeOneShot.OneShotRequest.Fire);
        }
      }
      var speedCap = new Vector3(Speed, TermVel, Speed);
      velocity = velocity.Clamp(-speedCap, speedCap);
      var animState = "Running";
      if (!IsOnFloor()) {
        animState = "Falling";
      } else if (inputDir == Vector2.Zero) {
        animState = "Idle";
      }

      _animTree.Set("parameters/Running/blend_position", Transform.Basis.Inverse() * velocity.Normalized());
      _animTree.Set("parameters/Transition/transition_request", animState);


      Velocity = velocity;
      MoveAndSlide();
      UpdateCursor();
      Chunk.Loader.SetGrassPlayer(Position);
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
