using Godot;
using System;
namespace Player {
  public partial class Cursor : MeshInstance3D {
    public MeshInstance3D Cube;
    private MeshInstance3D Sphere;
    private MeshInstance3D Cylinder;
    private StandardMaterial3D _previewMaterial;
    private Int32 UnitId;
    private Rid _space;

    public override void _Ready() {
      Cube = GetNode<MeshInstance3D>("Cube");
      Cube.Visible = false;
      Sphere = GetNode<MeshInstance3D>("Sphere");
      Sphere.Visible = false;
      Cylinder = GetNode<MeshInstance3D>("Cylinder");
      Cylinder.Visible = false;
      _space = GetWorld3D().Space;
    }
    public void Update(Vector3 pos, Vector3 facing, UnitStack stack) {
      if (stack.Amt == 0) {
        return;
      }
      if (stack.Id < Glob.Units.Count) {
        var unit = Glob.Units[stack.Id];
        switch (unit.Type) {
          case UnitType.Terraform:
          RayCastMesh(pos, facing, unit);
          break;
        }

      }
    }
    void RayCastMesh(Vector3 origin, Vector3 facing, Unit unit) {
      var ray = PhysicsRayQueryParameters3D
        .Create(origin, facing * 8 + origin);
      var result = PhysicsServer3D.SpaceGetDirectState(_space)
        .IntersectRay(ray);
      //{
      //position: Vector2 # point in world space for collision
      //normal: Vector2 # normal in world space for collision
      //collider: Object # Object collided or null (if unassociated)
      //collider_id: ObjectID # Object it collided against
      //rid: RID # RID it collided against
      //shape: int # shape index of collider
      //metadata: Variant() # metadata of collider
      //}
      if (!result.ContainsKey("normal")) {
        return;
      }
      Mesh = (unit.shape) switch
      {
        AoeShape.Cube => Cube.Mesh,
        AoeShape.Sphere => Sphere.Mesh,
        AoeShape.Cylinder => Cylinder.Mesh,
      };
      var norm = (Vector3)result["normal"];
      var pos = (Vector3)result["position"];
      //var ax = (Int32)norm.Abs().MaxAxisIndex();
      //var offset = Vector3.Zero;
      //offset[ax] = 0.5f * norm[ax] / Math.Abs(norm[ax]);
      //var scale = Vector3.One * 0.5f * Chunk.Geometry.Scale;
      //scale[ax] *= 2;
      Position = pos;//((pos / Chunk.Geometry.Scale).Round() + offset)
                     //* Chunk.Geometry.Scale;
                     //+ Vector3.One * Chunk.Geometry.Scale * 0.5f);
      Visible = true;

    }
  }
}
