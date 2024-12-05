using Godot;

public partial class Bindery : ColorRect {
<<<<<<< HEAD
  private UnitGrid _bank;
  private UnitGrid _spell;
  private UnitGrid _items;
=======
  private UnitSlots _bank;
  private UnitSlots _spell;
  private UnitSlots _items;
>>>>>>> 82d442f (first commit:)
  private Label _stats;
  private Hotbar _hotbar;
  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
<<<<<<< HEAD
    _bank = GetNode<UnitGrid>("CenterContainer/VBoxContainer/HBoxContainer/PhaseBankUnitGrid");
    _spell = GetNode<UnitGrid>("CenterContainer/VBoxContainer/HBoxContainer/PhaseUnitGrid");
    _items = GetNode<UnitGrid>("CenterContainer/VBoxContainer/HBoxContainer/ItemUnitGrid");
=======
    _bank = GetNode<UnitSlots>("CenterContainer/VBoxContainer/HBoxContainer/PhaseBankScrollContainer");
    _spell = GetNode<UnitSlots>("CenterContainer/VBoxContainer/HBoxContainer/PhaseScrollContainer");
    _items = GetNode<UnitSlots>("CenterContainer/VBoxContainer/HBoxContainer/ItemScrollContainer");
>>>>>>> 82d442f (first commit:)
    _hotbar = GetNode<Hotbar>("CenterContainer/VBoxContainer/Hotbar");
    _stats = GetNode<Label>("CenterContainer/VBoxContainer/HBoxContainer/VBoxContainer/SpellStats");
  }
  public void Populate(Player.Save save) {
    //_bank.BindStacks(save.Phases);
    _items.BindStacks(save.InventoryStacks);
    //_spell.SetOnDrop(UpdateSpell);
    _hotbar.BindStacks(save.HotbarStacks);
  }
<<<<<<< HEAD
=======
  public void UpdateStacks() {
    _items.UpdateStacks();
    _hotbar.UpdateStacks();
  }
>>>>>>> 82d442f (first commit:)
}
