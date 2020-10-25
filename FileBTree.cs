using System;
using System.Collections.Generic;
using System.IO;
using LanguageExt;
using RZ.Foundation.Extensions;
using static LanguageExt.Prelude;

namespace RZ.App
{
    public interface IBinarySerializer<T>
    {
        int BlockSize { get; }
        byte[] Serialize(T data);
        T Deserialize(byte[] binary);
    }

    public sealed class FileBTree<T> : IDisposable
    {
        readonly IBinarySerializer<T> serializer;
        readonly Func<T,T, int> comparer;
        readonly FileStream file;

        public FileBTree(string fileName, IBinarySerializer<T> serializer, Func<T,T, int> Comparer) {
            this.serializer = serializer;
            comparer = Comparer;
            file = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }

        public void Dispose() {
            file.Close();
            file.Dispose();
        }

        public FileBTree<T> Add(T data) {
            var root = GetRoot();
            if (root.IsNone)
                WriteNode(new BinaryFileNode<T>(0, data));
            else {
                var (parent, toInsert) = FindInsertionNode(root.Get(), data);
                toInsert.Link = AddNewNode(data, parent.Address).Address;
                UpdateNode(parent);
            }
            return this;
        }

        public IEnumerable<T> Traverse() {
            var root = GetRoot();
            if (root.IsNone) yield break;

            var node = root;
            var stack = new Stack<BinaryFileNode<T>>();
            do {
                do {
                    stack.Push(node.Get());
                    node = ReadNodeAt(node.Get().Left.Link);
                } while (node.IsSome);

                do {
                    node = stack.TryPop();
                    if (node.IsSome) {
                        yield return node.Get().Value;
                        node = ReadNodeAt(node.Get().Right.Link);
                    }
                    else
                        break;
                } while (node.IsNone);
            } while (node.IsSome);
        }

        public (BinaryFileNode<T>, BinaryFileNodeLink) FindInsertionNode(BinaryFileNode<T> current, T data) {
            var insertionPoint = FindInsertionPoint(current, data);
            var link = insertionPoint.Link;
            return link != BinaryFileNodeLink.Null ? FindInsertionNode(ReadNodeAt(link).Get(), data) : (current, insertionPoint);
        }

        public BinaryFileNodeLink FindInsertionPoint(BinaryFileNode<T> current, T data) =>
            comparer(data, current.Value) switch
            {
                0 => throw new InvalidOperationException("Duplicated data"),
                -1 => current.Left,
                1 => current.Right,
                _ => throw new InvalidOperationException("Unexpected comparison value")
            };

        #region File Operations

        Option<BinaryFileNode<T>> GetRoot() {
            if (file.Length == 0) return None;

            file.Seek(0, SeekOrigin.Begin);
            return ReadNode();
        }

        Option<BinaryFileNode<T>> ReadNodeAt(long offset) {
            if (offset == BinaryFileNodeLink.Null) return None;

            file.Seek(offset, SeekOrigin.Begin);
            return ReadNode();
        }

        BinaryFileNode<T> ReadNode() {
            var address = file.Position;
            var dataBlock = new byte[serializer.BlockSize];
            file.Read(dataBlock, 0, serializer.BlockSize);
            var data = serializer.Deserialize(dataBlock);

            var pointerBlock = new byte[sizeof(long)];
            var from = readLong();
            var left = readLong();
            var right = readLong();

            return new BinaryFileNode<T>(address, data, from, left, right);

            long readLong() {
                file.Read(pointerBlock, 0, pointerBlock.Length);
                return BitConverter.ToInt64(pointerBlock);
            }
        }

        void WriteNode(BinaryFileNode<T> node) {
            var dataBlock = serializer.Serialize(node.Value);
            file.Write(dataBlock, 0, dataBlock.Length);
            writeLong(node.From.Link);
            writeLong(node.Left.Link);
            writeLong(node.Right.Link);

            void writeLong(long v) {
                var block = BitConverter.GetBytes(v);
                file.Write(block, 0, block.Length);
            }
        }

        BinaryFileNode<T> AddNewNode(T data, long parent) {
            file.Seek(0, SeekOrigin.End);
            var address = file.Position;
            var newNode = new BinaryFileNode<T>(address, data, parent);
            WriteNode(newNode);
            return newNode;
        }

        void UpdateNode(BinaryFileNode<T> node) {
            file.Seek(node.Address, SeekOrigin.Begin);
            WriteNode(node);
        }

        #endregion
    }

    /// <summary>
    /// Binary file block
    /// </summary>
    /// <typeparam name="T">data type</typeparam>
    /// <remarks>
    /// Binary file block NodeOverheadSize + data size.. start from
    /// 1. Data block with size = data size
    /// 2. Fromt/Left/Right pointers, each has size = sizeof(long)
    /// </remarks>
    public sealed class BinaryFileNode<T>
    {
        const int NodeOverheadSize = sizeof(long) * 3;
        public long Address { get; }
        public T Value { get; }
        public BinaryFileNodeLink From { get; } = new BinaryFileNodeLink();
        public BinaryFileNodeLink Left { get; } = new BinaryFileNodeLink();
        public BinaryFileNodeLink Right { get; } = new BinaryFileNodeLink();

        public BinaryFileNode(long Address, T Value, long From = BinaryFileNodeLink.Null, long Left = BinaryFileNodeLink.Null,
                              long Right = BinaryFileNodeLink.Null) {
            (this.Address, this.Value) = (Address, Value);
            this.From.Link = From;
            this.Left.Link = Left;
            this.Right.Link = Right;
        }
    }

    public sealed class BinaryFileNodeLink
    {
        public const long Null = -1;
        public long Link { get; set; }
        public bool IsEmpty => Link == Null;
    }
}