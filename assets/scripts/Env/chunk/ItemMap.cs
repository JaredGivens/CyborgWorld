using System;
using Google.FlatBuffers;

namespace Chunk {
  public struct ItemMap {
    enum Status : Int16 {
      Tombstone = -1,
      Empty = -2,
    }
    private Int16 _tombstoneCount;
    public Int16 Count;
    public Int16[] Keys;
    public Int16[] Items;
    public ItemMap() {
      _tombstoneCount = 0;
      Count = 0;
      Keys = new Int16[4];
      Items = new Int16[4 * 16];
    }
    public Span<Int16> this[Int16 key] {
      get {
        var res = KeyIndexOf(key);
        if (res < 0) {
          throw new IndexOutOfRangeException();
        }
        return Items.AsSpan(res * 16, 16);
      }
      set {
        var res = KeyIndexOf(key);
        if (Keys[res] < 0) {
          if (Keys.Length != Int16.MaxValue && Count >= Keys.Length * 0.75f) {
            Rehash();
            res = KeyIndexOf(key);
          }
          value.CopyTo(Items.AsSpan(res * 16, 16));
          Keys[res] = key;
        } else {
          value.CopyTo(Items.AsSpan(res * 16, 16));
        }
      }
    }
    void Remove(Int16 key) {
      var res = KeyIndexOf(key);
      if (Keys[res] > 0) {
        Keys[res] = (Int16)Status.Tombstone;
        if (++_tombstoneCount >= Keys.Length * 0.25) {
          Rehash();
        }
      }
    }
    private Int16 KeyIndexOf(Int16 key) {
      var hashKey = (Int16)Glob.Mod2(key, Keys.Length);
      Int16 tombstoneIndex = -1;
      for (Int32 i = 1; i < Keys.Length; ++i) {
        if (Keys[hashKey] == (Int16)Status.Empty) {
          return tombstoneIndex == -1 ? hashKey : tombstoneIndex;
        } else if (tombstoneIndex == -1 && Keys[hashKey] == (Int16)Status.Tombstone) {
          tombstoneIndex = hashKey; // Mark the first tombstone index
        } else if (Keys[hashKey] == key) {
          return hashKey;
        }
        hashKey = (Int16)Glob.Mod2(hashKey + i * i, Keys.Length);
      }
      throw new IndexOutOfRangeException();
    }
    private void Rehash() {
      Int16 newCapacity = (Int16)(Keys.Length << Glob.DivFloor(Count, Keys.Length * 0.75f));
      Int16[] oldKeys = Keys;
      Int16[] oldItems = Items;
      Keys = new Int16[newCapacity];
      Array.Fill(Keys, (Int16)Status.Empty);
      Items = new Int16[newCapacity * 16];
      Count = 0;
      _tombstoneCount = 0;
      for (Int32 i = 0; i < oldKeys.Length; i++) {
        if (oldKeys[i] >= 0) {
          this[oldKeys[i]] = oldItems.AsSpan(i * 16, 16);
        }
      }
    }
    public void FromFb(Fb.Items items) {
      Count = items.Count;
      _tombstoneCount = items.TombstoneCount;
      Keys = items.GetKeysArray();
      Items = items.GetItemsArray();
    }
    public Offset<Fb.Items> BuildFb(FlatBufferBuilder fbb) {
      var keyVector = Fb.Items.CreateKeysVector(fbb, Keys);
      var itemVector = Fb.Items.CreateItemsVector(fbb, Items);
      Fb.Items.StartItems(fbb);
      Fb.Items.AddCount(fbb, Count);
      Fb.Items.AddTombstoneCount(fbb, _tombstoneCount);
      Fb.Items.AddKeys(fbb, keyVector);
      Fb.Items.AddItems(fbb, itemVector);
      return Fb.Items.EndItems(fbb);
    }
  }
}
