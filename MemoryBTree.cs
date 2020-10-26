using System;
using System.Collections.Generic;
using LanguageExt;
using RZ.Foundation.Extensions;
using static LanguageExt.Prelude;

namespace RZ.App
{
    public sealed class MemoryBTree<T>
    {
        readonly Func<T,T, int> comparer;

        Option<BinaryNode<T>> root = None;

        public MemoryBTree(Func<T,T, int> Comparer) => comparer = Comparer;

        public MemoryBTree<T> Add(T data) {
            if (root.IsNone)
                root = new BinaryNode<T>(data);
            else {
                var (parent, toInsert) = FindInsertionNode(root.Get(), data);
                toInsert.Link = new BinaryNode<T>(data, parent);
            }
            return this;
        }

        public IEnumerable<T> Traverse() {
            if (root.IsNone) yield break;

            var node = root;
            var stack = new Stack<BinaryNode<T>>();
            do {
                do {
                    stack.Push(node.Get());
                    node = node.Get().Left.Link;
                } while (node.IsSome);

                do {
                    node = stack.TryPop();
                    if (node.IsSome) {
                        yield return node.Get().Value;
                        node = node.Get().Right.Link;
                    }
                    else
                        break;
                } while (node.IsNone);
            } while (node.IsSome);
        }

        public (BinaryNode<T>, BinaryNodeLink<T>) FindInsertionNode(BinaryNode<T> current, T data) {
            var insertionPoint = FindInsertionPoint(current, data);
            var link = insertionPoint.Link;
            return link.IsSome? FindInsertionNode(link.Get(), data) : (current, insertionPoint);
        }

        public BinaryNodeLink<T> FindInsertionPoint(BinaryNode<T> current, T data) =>
            comparer(data, current.Value) switch
            {
                0 => throw new InvalidOperationException("Duplicated data"),
                -1 => current.Left,
                1 => current.Right,
                _ => throw new InvalidOperationException("Unexpected comparison value")
            };
    }

    public sealed class BinaryNode<T>
    {
        public T Value { get; }
        public BinaryNodeLink<T> From { get; } = new BinaryNodeLink<T>();
        public BinaryNodeLink<T> Left { get; } = new BinaryNodeLink<T>();
        public BinaryNodeLink<T> Right { get; } = new BinaryNodeLink<T>();

        public BinaryNode(T Value, BinaryNode<T>? From = null) {
            this.Value = Value;
            this.From.Link = From ?? Option<BinaryNode<T>>.None;
        }
    }

    public sealed class BinaryNodeLink<T>
    {
        public Option<BinaryNode<T>> Link { get; set; } = None;
        public bool IsEmpty => Link.IsNone;
    }
}