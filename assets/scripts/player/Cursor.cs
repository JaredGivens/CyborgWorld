using Godot;
using System;
namespace Player {
  public partial class Cursor : Node3D {
    public MeshInstance3D Block;
    private MeshInstance3D Sphere;
    private StandardMaterial3D _previewMaterial;
    private Int32 UnitId;
    private Rid _space;

    public override void _Ready() {
      Block = GetNode<MeshInstance3D>("Block");
      Sphere = GetNode<MeshInstance3D>("Sphere");
      _space = GetWorld3D().Space;
    }
    public void Update(Vector3 pos, Vector3 facing, UnitStack stack) {
      if (Sphere != null) {
        Sphere.Visible = false;
      }
      if (Block != null) {
        Block.Visible = false;
      }
      if (stack.Amt == 0) {
        return;
      }
      if (stack.Id < Glob.Units.Count) {
        switch (Glob.Units[stack.Id].Type) {
          case UnitType.Block:
          UpdateBlock(pos, facing);
          break;
        }

      }
    }
    void UpdateBlock(Vector3 origin, Vector3 facing) {
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
        Block.Visible = false;
        return;
      }
      var norm = (Vector3)result["normal"];
      var pos = (Vector3)result["position"];
      var ax = (Int32)norm.Abs().MaxAxisIndex();
      var offset = Vector3.Zero;
      offset[ax] = 0.5f * norm[ax] / Math.Abs(norm[ax]);
      var scale = Vector3.One * 0.5f * Chunk.Geometry.Scale;
      scale[ax] *= 2;
      Block.GlobalTransform =
        new Transform3D(Basis.FromScale(scale),
            ((pos / Chunk.Geometry.Scale).Round() + offset)
            * Chunk.Geometry.Scale);
      //+ Vector3.One * Chunk.Geometry.Scale * 0.5f);
      Block.Visible = true;

    }
  }
}
