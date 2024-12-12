using Godot;
using System;

public partial class UnitTexture : TextureRect {
  private Boolean _mutable;
  private UnitType _types;
  private Action OnChange;
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
  public void Init(UnitStack initialValue, Boolean mutable, UnitType types, Action onChange) {
    _types = types;
    _mutable = mutable;
    OnChange = onChange;
    Stack = initialValue;
  }
  public override Variant _GetDragData(Vector2 at_position) {
    if (Stack.Amt == 0) {
      return (Int16)(0);
    }
    var tr = new TextureRect();
    tr.Texture = Texture;
    tr.ExpandMode = ExpandMode;
    tr.CustomMinimumSize = CustomMinimumSize;
    tr.Position = -(CustomMinimumSize / 2);
    SetDragPreview(tr);
    //if (_mutable) {
    //Stack = new UnitStack(Stack.Id, Stack.Amt - 1);
    //}
    _dragged = this;
    return (Int16)new UnitStack(Stack.Id, 1);
  }
  private void Update() {
    if (OnChange != null) {
      OnChange();
    }
    if (Stack.Amt == 0) {
      Texture = null;
      TooltipText = "";
      _countLabel.Text = "";
      return;
    }
    var item = Glob.Units[Stack.Id];
    Texture = item.Icon;
    TooltipText = item.Desc;
    if (Stack.Amt > 1) {
      _countLabel.Text = Stack.Amt.ToString();
    }
  }
  public override Boolean _CanDropData(Vector2 at_position, Variant data) {
    var stack = (UnitStack)data;
    if (stack.Amt == 0) {
      return false;
    }
    var type = Glob.Units[stack.Id].Type;
    if ((type & _types) == UnitType.None) {
      return false;
    }
    return _mutable;
  }
  public override void _DropData(Vector2 at_position, Variant data) {
    //if (OnDrop != null) {
    //OnDrop();
    //}
    if (_dragged.Stack.Amt == 1) {
      _dragged.Stack = Stack;
    }
    Stack = (UnitStack)data;
  }
  public override void _Notification(Int32 code) {
    //if (code != NotificationDragEnd || this != _dragged) {
    //return;
    //}
    //if (_mutable && IsDragSuccessful()) {
    //return;
    //}
    //Stack = new UnitStack(Stack.Id, Stack.Amt + 1);

  }
}
