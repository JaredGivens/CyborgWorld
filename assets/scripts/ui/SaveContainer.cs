using System;
using Godot;
using System.IO;

public partial class SaveContainer : HBoxContainer {
  // Called when the node enters the scene tree for the first time.
  private Label _label;
  private Button _playButton;
  private Button _deleteButton;
  public Boolean Pressed;
  public World.Save _save;
  public String _path;
  public override void _Ready() {
    _label = GetNode<Label>("Label");
    _playButton = GetNode<Button>("PlayButton");
    _deleteButton = GetNode<Button>("DeleteButton");
  }
  public void Play() {
    Pressed = true;
  }
  public void Delete() {
    Directory.Delete(_path, true);
    GetParent().RemoveChild(this);
  }
  public void From(string path) {
    var name = Path.GetFileName(path);
    _path = path;
    _save = GD.Load<World.Save>($"{path}/{name}.tres");
    _label.Text = name;
    _playButton.Pressed += Play;
    _deleteButton.Pressed += Delete;
  }
}
