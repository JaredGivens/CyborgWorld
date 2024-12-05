using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Google.FlatBuffers;
using System.Threading;
using System.Buffers;
using System.IO.Compression;
using System.Linq;
using Godot;
using System.Collections;
using System.Runtime.InteropServices;

namespace Chunk {
  public class RegionHandle {
    private Region _region = new();
    private ReaderWriterLockSlim _rwlock = new();
    public void Use(Action<Region> action) {
      _rwlock.EnterReadLock();
      action(_region);
      _rwlock.ExitReadLock();
    }
    public void StoreLoad(Vector3I rkey, Action<Region> action) {
      _rwlock.EnterReadLock();
      if (_region.Rkey == rkey) {
        action(_region);
        _rwlock.ExitReadLock();
        return;
      }
      _rwlock.ExitReadLock();
      _rwlock.EnterUpgradeableReadLock();
      if (_region.Rkey == rkey) {
        _rwlock.ExitUpgradeableReadLock();
        _rwlock.EnterReadLock();
        action(_region);
        _rwlock.ExitReadLock();
        return;
      }
      _rwlock.EnterWriteLock();
      _region.Store();
      _region.Load(rkey);
      _rwlock.ExitWriteLock();
      _rwlock.ExitUpgradeableReadLock();
      _rwlock.EnterReadLock();
      action(_region);
      _rwlock.ExitReadLock();
    }
  }
  record struct Location(Int16 BitArr, Int16 Bit, Int16 PageCount = 0);
  public class Region {
    private const Int32 _pageBytes = 4096;
    private const Int32 _headerBytes = 8192;
    private const Int32 _shardCount = 8;
    private const Int32 _bitArrayCount = DimLen3 / _shardCount;
    private const Int32 _bitCount = 8192; // > chunk max pages
    public const Int32 DimLen = 8;
    public const Int32 DimLen2 = DimLen * DimLen;
    public const Int32 DimLen3 = DimLen * DimLen2;
    public const Int32 MapDimLen = 2;
    public const Int32 MapDimLen2 = MapDimLen * MapDimLen;
    public const Int32 MapDimLen3 = MapDimLen * MapDimLen2;
    FileStream _fs;
    MemoryMappedFile _mmf;
    private Location[] _locations;
    private BitArray[][] _freeShards;
    public Vector3I Rkey = Vector3I.MaxValue;
    public void Load(Vector3I rkey) {
      Rkey = rkey;
      var fn = FileName();
      _locations = new Location[DimLen3];
      _freeShards = Enumerable.Range(0, _shardCount)
        .Select(_ => Enumerable.Range(0, _bitArrayCount)
        .Select(_ => new BitArray(_bitCount)).ToArray()).ToArray();
      _fs = File.Exists(fn) switch
      {
        true => OpenFile(fn),
        false => File.Open(fn, FileMode.Create,
            System.IO.FileAccess.ReadWrite, FileShare.ReadWrite),
      };
      _mmf = MemoryMappedFile.CreateFromFile(_fs, null,
            (Int64)DimLen3 * _bitCount * _pageBytes + _headerBytes,
            MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true);
    }
    public bool HasChunk(Int32 rflat) {
      return _locations[rflat].PageCount != 0;
    }
    private FileStream OpenFile(String fn) {
      var fs = File.Open(fn, FileMode.Open,
          System.IO.FileAccess.ReadWrite, FileShare.ReadWrite);
      var buf = ArrayPool<Byte>.Shared.Rent(_headerBytes);
      fs.Read(buf, 0, _headerBytes);
      var bb = new ByteBuffer(buf);
      var header = Fb.Header.GetRootAsHeader(bb);
      if (DimLen3 == header.LocationsLength) {
        for (Int32 i = 0; i < DimLen3; ++i) {
          var loc = new Location()
          {
            PageCount = header.Locations(i).Value.PageCount,
            BitArr = header.Locations(i).Value.BitArr,
            Bit = header.Locations(i).Value.Bit,
          };
          _locations[i] = loc;
          for (Int32 j = 0; j < loc.PageCount; ++j) {
            _freeShards[Shard(i)][loc.BitArr][j + loc.Bit] = true;
          }
        }
      }
      ArrayPool<Byte>.Shared.Return(buf);
      return fs;
    }
    private Int32 Shard(Int32 rflat) {
      return Glob.ModFlat(Glob.Unflat(rflat, DimLen), 2); // 2 ** 3 == 8 == _shardCount
    }
    public void GetChunk(Int32 rflat, ref Durable durable) {
      if (_mmf == null) {
        return;
      }
      var shard = Shard(rflat);
      var loc = _locations[rflat];
      var tmp = ArrayPool<Byte>.Shared.Rent(loc.PageCount * _pageBytes);
      using (var vs = ChunkView(shard, loc)) {
        vs.Read(tmp, 0, loc.PageCount * _pageBytes);
      }
      Byte[] buf;
      using (var ms = new MemoryStream()) {
        using (var zs = new ZLibStream(new MemoryStream(tmp), CompressionMode.Decompress)) {
          zs.CopyTo(ms);
        }
        buf = ms.ToArray();
      }
      ArrayPool<Byte>.Shared.Return(tmp);
      var bb = new ByteBuffer(buf);
      var chunk = Fb.ChunkSave.GetRootAsChunkSave(bb);
      chunk.GetCellsArray().CopyTo(durable.Cells, 0);
    }
    Location AddLocation(Int32 rflat, Int32 shard, Int16 pageCount) {
      for (Int16 i = 0; i < _bitArrayCount; ++i) {
        var s = _freeShards[shard];
        var bitArray = s[i];
        var freePages = 0;
        lock (bitArray) {
          for (Int16 j = 0; j <= _bitCount; j++) {
            if (bitArray[j]) {
              freePages = 0;
            } else {
              ++freePages;
            }
            if (freePages != pageCount) {
              continue;
            }
            for (Int16 k = 0; k < pageCount; ++k) {
              bitArray[j - k] = true;
            }
            return _locations[rflat] = new Location()
            {
              BitArr = i,
              Bit = (Int16)(j - pageCount + 1),
              PageCount = pageCount,
            };
          }
        }
      }
      return new Location();  // Return -1 if no contiguous block found
    }
    Location UpdateLocation(Int32 rflat, Int32 shard, Int16 pageCount) {
      if (_locations[rflat].PageCount == 0) {
        return AddLocation(rflat, shard, pageCount);
      }
      var loc = _locations[rflat];
      if (loc.PageCount < pageCount) {
        // free all old pages
        var bitArr = _freeShards[shard][loc.BitArr];
        lock (bitArr) {
          for (Int32 i = 0; i < loc.PageCount; ++i) {
            bitArr[i + loc.Bit] = false;
          }
        }
        _locations[rflat].PageCount = 0;
        return AddLocation(rflat, shard, pageCount);
      }
      if (loc.PageCount > pageCount) {
        // free pages no longer needed
        var diff = loc.PageCount - pageCount;
        var bitArr = _freeShards[shard][loc.BitArr];
        lock (bitArr) {
          for (Int32 i = 0; i < diff; ++i) {
            bitArr[i + loc.Bit + pageCount - diff] = false;
          }
        }
        _locations[rflat].PageCount = pageCount;
        return loc;
      }
      return loc;
    }
    MemoryMappedViewStream ChunkView(Int64 shard, Location loc) {
      return _mmf.CreateViewStream(
          ((shard + loc.BitArr * _shardCount) * _bitCount + loc.Bit)
          * _pageBytes + _headerBytes, loc.PageCount * _pageBytes);
    }

    public void SetChunk(Int32 rflat, ref Durable durable) {
      if (_mmf == null) {
        return;
      }
      var fbb = new FlatBufferBuilder(new ByteBuffer(_pageBytes));
      var cellsVector = Fb.ChunkSave.CreateCellsVector(fbb, durable.Cells);
      var itemsVector = Fb.ChunkSave.CreateItemsVector(fbb, durable.Items);
      Fb.ChunkSave.StartChunkSave(fbb);
      Fb.ChunkSave.AddCells(fbb, cellsVector);
      Fb.ChunkSave.AddItems(fbb, itemsVector);
      var chunk = Fb.ChunkSave.EndChunkSave(fbb);
      fbb.Finish(chunk.Value);
      var arr = fbb.SizedByteArray();
      using (var ms = new MemoryStream()) {
        using (var zs = new ZLibStream(ms, CompressionMode.Compress, true)) {
          zs.Write(arr, 0, arr.Length);
        }
        ms.Position = 0;
        var pageCount = (Int16)Math.Ceiling((Single)ms.Length / _pageBytes);
        var shard = Shard(rflat);
        var loc = UpdateLocation(rflat, shard, pageCount);
        using (var vs = ChunkView(shard, loc)) {
          ms.WriteTo(vs);
        }
      }
    }
    public void Store() {
      if (_mmf == null) {
        return;
      }
      var buf = ArrayPool<Byte>.Shared.Rent(_headerBytes);
      var fbb = new FlatBufferBuilder(new ByteBuffer(buf));
      var offsets = new Offset<Fb.Location>[DimLen3];
      Fb.Header.StartLocationsVector(fbb, DimLen3);
      for (Int32 i = DimLen3 - 1; i > -1; --i) {
        var loc = _locations[i];
        Fb.Location.CreateLocation(fbb, loc.BitArr, loc.Bit, loc.PageCount);
      }
      var locVector = fbb.EndVector();
      Fb.Header.StartHeader(fbb);
      Fb.Header.AddLocations(fbb, locVector);
      var header = Fb.Header.EndHeader(fbb);
      fbb.Finish(header.Value);
      var arr = fbb.SizedByteArray();
      using (var vs = _mmf.CreateViewStream(0, _headerBytes)) {
        vs.Write(arr, 0, arr.Length);
      }
      ArrayPool<Byte>.Shared.Return(buf);
      _mmf.Dispose();
      _mmf = null;
      _fs.Close();
    }
    public String FileName() {
      return $"{Glob.SavePath}/regions/r.{Rkey.X}.{Rkey.Y}.{Rkey.Z}.bin";
    }
  }
}
