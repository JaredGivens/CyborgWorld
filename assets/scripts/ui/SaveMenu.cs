using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
public enum GamemodeEnum {
  Survival,
  Sandbox,
}

public partial class SaveMenu : Node3D {
  [Export]
  private AnimationPlayer _animPlayer;
  [Export]
  private LineEdit _nameLineEdit;
  [Export]
  private LineEdit _seedLineEdit;
  [Export]
  private Button _buildSaveButton;
  [Export]
  private OptionButton _gamemodeOption;
  [Export]
  private VBoxContainer _saveVBox;
  private String _saveDir;
  private System.Random _rng = new();
  public List<SaveContainer> SaveContainers = new();


  private PackedScene _saveContainerPacked = GD.Load<PackedScene>("res://scenes/ui/save_container.tscn");
  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    _animPlayer.Play("idle");
    _buildSaveButton.Pressed += BuildSave;
    _saveDir = ProjectSettings.GlobalizePath("user://saves");
    if (!Directory.Exists(_saveDir)) {
      Directory.CreateDirectory(_saveDir);
    } else {
      LoadSaves();
    }
  }
  private void BuildSave() {
    if (!Regex.IsMatch(_nameLineEdit.Text, @"[\w.-]{1,63}$")) {
      GD.PrintErr(_nameLineEdit.Text, " name does not match pattern");
      return;
    }
    if (Directory.Exists($"{_saveDir}/{_nameLineEdit.Text}")) {
      GD.PrintErr(_nameLineEdit.Text, " save name conflict");
      return;
    }
    var save = new World.Save();
    save.Name = _nameLineEdit.Text;
    if (_seedLineEdit.Text == "") {
      save.Seed = _rng.Next();

    } else if (Regex.IsMatch(_seedLineEdit.Text, @"^-?\d+$")) {
      save.Seed = int.Parse(_seedLineEdit.Text);
    } else {
      GD.PrintErr("seed does not match pattern");
      return;
    }
    save.Gamemode = (GamemodeEnum)_gamemodeOption.Selected;
    GD.Print(save.Gamemode);
    var savePath = $"{_saveDir}/{save.Name}";
    Directory.CreateDirectory(savePath);
    Directory.CreateDirectory($"{savePath}/regions");
    Directory.CreateDirectory($"{savePath}/player_data");
    ResourceSaver.Save(save, $"user://saves/{save.Name}/{save.Name}.tres");
    _nameLineEdit.Clear();
    _seedLineEdit.Clear();
    AddSave(savePath);
  }
  private void LoadSaves() {
    foreach (var savePath in Directory.GetDirectories(_saveDir)) {
      AddSave(savePath);
    }
  }
  private void AddSave(string savePath) {
    var saveContainer = _saveContainerPacked.Instantiate<SaveContainer>();
    SaveContainers.Add(saveContainer);
    _saveVBox.AddChild(saveContainer);
    saveContainer.From(savePath);
  }

}
