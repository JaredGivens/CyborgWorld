using Godot;
using System;
public class Mob {
  public Condition Condition = new();
  public PrioEnum Prio;
  private CharacterBody3D Body;
  private Vector3[] _path;
  private int _pathI = 0;
  public Mob(CharacterBody3D body) {
    Body = body;
  }
  public void pathTwards(Vector3[] path) {
    _path = path;
    _pathI = 0;
  }
  public void UpdatePrio() {
  }
  public void FollowPath(float speed) {
    if (_path == null) {
      return;
    }
    var floor_pos = Body.Position.Floor();
    if (floor_pos.IsEqualApprox(_path[_pathI])) {
      if (++_pathI == _path.Length) {
        _path = null;
        return;
      }
    }

    Body.Velocity = Body.GlobalTransform
        .Origin
        .DirectionTo(_path[_pathI]) * speed;
    Body.MoveAndSlide();
  }
  public void Process(float speed) {
    FollowPath(speed);
  }
}
