using Godot;

public partial class Bindery : ColorRect {
  private UnitSlots _bank;
  private UnitSlots _spell;
  private UnitSlots _items;
  private Label _stats;
  private Hotbar _hotbar;
  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    _bank = GetNode<UnitSlots>("CenterContainer/VBoxContainer/HBoxContainer/PhaseBankScrollContainer");
    _spell = GetNode<UnitSlots>("CenterContainer/VBoxContainer/HBoxContainer/PhaseScrollContainer");
    _items = GetNode<UnitSlots>("CenterContainer/VBoxContainer/HBoxContainer/ItemScrollContainer");
    _hotbar = GetNode<Hotbar>("CenterContainer/VBoxContainer/Hotbar");
    _stats = GetNode<Label>("CenterContainer/VBoxContainer/HBoxContainer/VBoxContainer/SpellStats");
  }
  public void Populate(Player.Save save) {
    //_bank.BindStacks(save.Phases);
    _items.BindStacks(save.InventoryStacks);
    //_spell.SetOnDrop(UpdateSpell);
    _hotbar.BindStacks(save.HotbarStacks);
  }
  public void UpdateStacks() {
    _items.UpdateStacks();
    _hotbar.UpdateStacks();
  }
}
