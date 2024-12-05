using Godot;
using System;

public partial class UnitTexture : TextureRect {
  public bool Mutable;
  public UnitType Types;
  //public Action OnDrop;
  private UnitStack _stack;
  public UnitStack Stack {
    get => _stack;
    set {
      _stack = value;
      Update();
    }
  }
  private Label _countLabel;
  private static UnitTexture _dragged;
  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    _countLabel = GetNode<Label>("CountLabel");
  }
  public override Variant _GetDragData(Vector2 at_position) {
    if (_stack.Amt == 0) {
      return Variant.From<Int16>(0);
    }
    var tr = new TextureRect();
    tr.Texture = Texture;
    tr.ExpandMode = ExpandMode;
    tr.CustomMinimumSize = CustomMinimumSize;
    tr.Position = -(CustomMinimumSize / 2);
    SetDragPreview(tr);
    if (Mutable) {
      Stack = new UnitStack(_stack.Id, _stack.Amt - 1);
    }
    _dragged = this;
    return Variant.From<Int16>((Int16)_stack);
  }
  private void Update() {
    if (_stack.Amt == 0) {
      Texture = null;
      TooltipText = "";
      _countLabel.Text = "";
      return;
    }
    var item = Glob.Units[_stack.Id];
    Texture = item.Icon;
    TooltipText = item.Desc;
    if (_stack.Amt > 1) {
      _countLabel.Text = _stack.Amt.ToString();
    }
  }
  public override bool _CanDropData(Vector2 at_position, Variant data) {
    //if (()data == 0) {
    //return false;
    //}
    var stack = (UnitStack)data;
    var type = Glob.Units[stack.Id].Type;
    if ((type & Types) == UnitType.None) {
      return false;
    }
    return Mutable;
  }
  public override void _DropData(Vector2 at_position, Variant data) {
    //if (OnDrop != null) {
    //OnDrop();
    //}
    if (_dragged.Stack.Amt == 1) {
      _dragged.Stack = _stack;
    }
    Stack = (UnitStack)data;
  }
  public override void _Notification(Int32 code) {
    if (code != NotificationDragEnd || this != _dragged || IsDragSuccessful()) {
      return;
    }
    Stack = new UnitStack(Stack.Id, Stack.Amt + 1);

  }
}
