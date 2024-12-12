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
    private Unit _defaultUnit;

    public override void _Ready() {
      Cube = GetNode<MeshInstance3D>("Cube");
      Cube.Visible = false;
      Sphere = GetNode<MeshInstance3D>("Sphere");
      Sphere.Visible = false;
      Cylinder = GetNode<MeshInstance3D>("Cylinder");
      Cylinder.Visible = false;
      _space = GetWorld3D().Space;
      _defaultUnit = new();
      _defaultUnit.Basis = Basis.Scaled(Vector3.One * 0.5f);
      _defaultUnit.Shape = AoeShape.Sphere;
    }
    public void Update(Vector3 pos, Vector3 facing, UnitStack stack) {
      Visible = false;
      if (stack.Amt == 0) {
        CastSnapMesh(pos, facing, _defaultUnit);
      } else {
        var unit = Glob.Units[stack.Id];
        switch (unit.Type) {
          case UnitType.Terraform:
          RayCastMesh(pos, facing, unit);
          break;
        }
      }
    }
    void CastSnapMesh(Vector3 origin, Vector3 facing, Unit unit) {
      var ray = PhysicsRayQueryParameters3D
        .Create(origin, facing * 32 + origin);
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
      GlobalBasis = unit.Basis;
      Mesh = (unit.Shape) switch
      {
        AoeShape.Cube => Cube.Mesh,
        AoeShape.Sphere => Sphere.Mesh,
        AoeShape.Cylinder => Cylinder.Mesh,
      };
      var norm = (Vector3)result["normal"];
      var pos = (Vector3)result["position"];
      GlobalPosition = (pos / Chunk.Geometry.Scale).Round();
      GlobalPosition += Vector3.One * 0.5f;
      GlobalPosition *= Chunk.Geometry.Scale;
      Visible = true;

    }
    void RayCastMesh(Vector3 origin, Vector3 facing, Unit unit) {
      var ray = PhysicsRayQueryParameters3D.Create(origin, facing * 8 + origin);
      var pdss = PhysicsServer3D.SpaceGetDirectState(_space);
      var result = pdss.IntersectRay(ray);
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
      GlobalBasis = unit.Basis;
      GlobalPosition = (Vector3)result["position"];
      Visible = true;
      Mesh = (unit.Shape) switch
      {
        AoeShape.Cube => Cube.Mesh,
        AoeShape.Sphere => Sphere.Mesh,
        AoeShape.Cylinder => Cylinder.Mesh,
      };

    }
  }
}
