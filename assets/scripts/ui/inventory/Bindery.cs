using Godot;

public partial class Bindery : ColorRect {
  private UnitGrid _bank;
  private UnitGrid _spell;
  private UnitGrid _items;
  private Label _stats;
  private Hotbar _hotbar;
  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    _bank = GetNode<UnitGrid>("CenterContainer/VBoxContainer/HBoxContainer/PhaseBankUnitGrid");
    _spell = GetNode<UnitGrid>("CenterContainer/VBoxContainer/HBoxContainer/PhaseUnitGrid");
    _items = GetNode<UnitGrid>("CenterContainer/VBoxContainer/HBoxContainer/ItemUnitGrid");
    _hotbar = GetNode<Hotbar>("CenterContainer/VBoxContainer/Hotbar");
    _stats = GetNode<Label>("CenterContainer/VBoxContainer/HBoxContainer/VBoxContainer/SpellStats");
  }
  public void Populate(Player.Save save) {
    //_bank.BindStacks(save.Phases);
    _items.BindStacks(save.InventoryStacks);
    //_spell.SetOnDrop(UpdateSpell);
    _hotbar.BindStacks(save.HotbarStacks);
  }
}
