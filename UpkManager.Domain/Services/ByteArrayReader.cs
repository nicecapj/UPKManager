﻿using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using ManagedLZO;

using UpkManager.Domain.Contracts;


namespace UpkManager.Domain.Services {

  [Export(typeof(IByteArrayReader))]
  [PartCreationPolicy(CreationPolicy.NonShared)]
  public class ByteArrayReader : IByteArrayReader {

    #region Private Fields

    private byte[] data;

    private int index;

    #endregion Private Fields

    #region IByteArrayReader Implementation

    public byte[] GetByteArray() {
      return data;
    }

    public IByteArrayReader CreateNew(byte[] Data, int Index) {
      ByteArrayReader reader = new ByteArrayReader();

      if (Index < 0 || Index >= Data.Length) throw new ArgumentOutOfRangeException(nameof(Index), "Index value is outside the bounds of the byte array.");

      reader.Initialize(Data, Index);

      return reader;
    }

    public void Initialize(byte[] Data, int Index) {
      data = Data;

      if (Index < 0 || Index >= data.Length) throw new ArgumentOutOfRangeException(nameof(Index), "Index value is outside the bounds of the byte array.");

      index = Index;
    }

    public void Seek(int Index) {
      if (Index < 0 || Index >= data.Length) throw new ArgumentOutOfRangeException(nameof(Index), "Index value is outside the bounds of the byte array.");

      index = Index;
    }

    public void Skip(int Count) {
      if (index + Count < 0 || index + Count >= data.Length) throw new ArgumentOutOfRangeException(nameof(Count), "Index + Count is out of the bounds of the byte array.");

      index += Count;
    }

    public IByteArrayReader Branch(int Offset) {
      ByteArrayReader reader = new ByteArrayReader();

      if (Offset < 0 || Offset >= data.Length) throw new ArgumentOutOfRangeException(nameof(Offset), "Index value is outside the bounds of the byte array.");

      reader.Initialize(data, Offset);

      return reader;
    }

    public async Task<IByteArrayReader> ReadByteArray(int Length) {
      if (index + Length < 0 || index + Length > data.Length) throw new ArgumentOutOfRangeException(nameof(Length), "Index + Length is out of the bounds of the byte array.");

      ByteArrayReader reader = new ByteArrayReader();

      reader.Initialize(await ReadBytes(Length), 0);

      return reader;
    }

    public async Task<IByteArrayReader> Splice(int Offset, int Length) {
      if (Offset + Length < 0 || Offset + Length > data.Length) throw new ArgumentOutOfRangeException(nameof(Offset), "Offset + Length is out of the bounds of the byte array.");

      ByteArrayReader reader = new ByteArrayReader();

      reader.Initialize(await ReadBytes(Offset, Length), 0);

      return reader;
    }

    public async Task Decrypt() {
      if (data.Length < 32) return;

//    const string key = "qiffjdlerdoqymvketdcl0er2subioxq";

      byte[] key = { 0x71, 0x69, 0x66, 0x66, 0x6a, 0x64, 0x6c, 0x65, 0x72, 0x64, 0x6f, 0x71, 0x79, 0x6d, 0x76, 0x6b, 0x65, 0x74, 0x64, 0x63, 0x6c, 0x30, 0x65, 0x72, 0x32, 0x73, 0x75, 0x62, 0x69, 0x6f, 0x78, 0x71 };

      await Task.Run(() => { for(int i = 0; i < data.Length; ++i) data[i] ^= key[i % 32]; });
    }

    public async Task<byte[]> Decompress(int UncompressedSize) {
      byte[] decompressed = new byte[UncompressedSize];

      await Task.Run(() => MiniLZO.Decompress(data, decompressed));

      return decompressed;
    }

    public short ReadInt16() {
      short value = BitConverter.ToInt16(data, index); index += sizeof(short);

      return value;
    }

    public ushort ReadUInt16() {
      ushort value = BitConverter.ToUInt16(data, index); index += sizeof(ushort);

      return value;
    }

    public int ReadInt32() {
      int value = BitConverter.ToInt32(data, index); index += sizeof(int);

      return value;
    }

    public uint ReadUInt32() {
      uint value = BitConverter.ToUInt32(data, index); index += sizeof(uint);

      return value;
    }

    public long ReadInt64() {
      long value = BitConverter.ToInt64(data, index); index += sizeof(long);

      return value;
    }

    public ulong ReadUInt64() {
      ulong value = BitConverter.ToUInt64(data, index); index += sizeof(ulong);

      return value;
    }

    public async Task<byte[]> ReadBytes(int Length) {
      if (Length == 0) return new byte[0];

      if (index + Length < 0 || index + Length > data.Length) throw new ArgumentOutOfRangeException(nameof(Length), "Index + Length is out of the bounds of the byte array.");

      byte[] value = new byte[Length];

      await Task.Run(() => { Array.ConstrainedCopy(data, index, value, 0, Length); index += Length; });

      return value;
    }

    public async Task<byte[]> ReadBytes(int Offset, int Length) {
      if (Offset + Length < 0 || Offset + Length > data.Length) throw new ArgumentOutOfRangeException(nameof(Offset), "Offset + Length is out of the bounds of the byte array.");

      byte[] value = new byte[Length];

      await Task.Run(() => Array.ConstrainedCopy(data, Offset, value, 0, Length));

      return value;
    }

    #endregion IByteArrayReader Implementation

  }

}
