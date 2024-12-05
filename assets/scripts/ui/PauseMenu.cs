using Godot;
using System;
using System.Threading.Tasks;
public partial class PauseMenu : ColorRect {
  private Button _resumeButton;
  private Button _exitGameButton;
  private Button _mainMenuButton;
  private Game _game;
  private static PackedScene _saveMenu = GD.Load<PackedScene>("res://scenes/ui/save_menu.tscn");
  public override void _Ready() {
    _mainMenuButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/VBoxContainer/MainMenuButton");
    _exitGameButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/VBoxContainer/ExitGameButton");
  }
  public void Init(Game game) {
    _mainMenuButton.Pressed += () => {
      game.StoreHome();
    };
    _exitGameButton.Pressed += () => {
      game.StoreExit();
    };
  }
}
