using Godot;
using System;

namespace Chunk {
  public partial class Block : Resource {
    [Export]
    public String AlbedoFn;
    [Export]
    public Chunk.BlockId BlockId;
    [Export]
    public Chunk.BlockMapping Mapping;
    [Export]
    public PackedScene? Ui;
  }
}
