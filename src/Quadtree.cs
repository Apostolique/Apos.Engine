using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Apos.Engine
{
    public interface IAABB
    {
        Rectangle AABB { get; }
    }

    /// <summary>For fast and accurate spatial partitioning. Set <see cref="Bounds"/> before use</summary>
    public static class Quadtree<T> where T : IAABB
    {
        public static Rectangle Bounds
        {
            get => _mainNode.Bounds;
            set
            {
                var items = _stored.Keys.ToArray();
                _mainNode.FreeSubNodes();
                _mainNode.Reset();
                _mainNode.Bounds = value;
                foreach (var i in items)
                    _stored[i] = _mainNode.Add(i);
                Pool<Node>.EnsureSize((int)MathF.Ceiling(value.Width / 32f * value.Height / 32f));
            }
        }

        public static readonly IDictionary<T, Node> _stored = new Dictionary<T, Node>();

        /// <summary>Returns true if <paramref name="item"/> is in the tree</summary>
        public static bool Contains(T item) => _stored.ContainsKey(item);

        readonly static Node _mainNode = new Node();

        static (T Item, Point HalfSize, Point Size) _maxSizeAABB;

        /// <summary>Insert (<paramref name="item"/>) into the tree</summary>
        public static void Add(T item)
        {
            _stored.Add(item, _mainNode.Add(item));
            if (item.AABB.Width > _maxSizeAABB.Size.X || item.AABB.Height > _maxSizeAABB.Size.Y)
                _maxSizeAABB = (item, new Point((int)MathF.Ceiling(item.AABB.Width / 2f), (int)MathF.Ceiling(item.AABB.Height / 2f)), new Point(item.AABB.Width, item.AABB.Height));
            var iCenter = item.AABB.Center;
            if (Bounds.Left > iCenter.X || Bounds.Top > iCenter.Y || Bounds.Right < iCenter.X + 1 || Bounds.Bottom < iCenter.Y + 1)
            {
                int newLeft = Math.Min(Bounds.Left, iCenter.X),
                    newTop = Math.Min(Bounds.Top, iCenter.Y),
                    newWidth = Bounds.Right - newLeft,
                    newHeight = Bounds.Bottom - newTop;
                Bounds = new Rectangle(newLeft, newTop, Math.Max(newWidth, iCenter.X - newLeft + 1), Math.Max(newHeight, iCenter.Y - newTop + 1));
            }
        }
        /// <summary>Remove (<paramref name="item"/>) from the tree</summary>
        public static void Remove(T item)
        {
            _stored[item].Remove(item);
            _stored.Remove(item);
            if (ReferenceEquals(item, _maxSizeAABB.Item))
            {
                _maxSizeAABB = (default, Point.Zero, Point.Zero);
                foreach (T i in _stored.Keys)
                    if (i.AABB.Width > _maxSizeAABB.Size.X || i.AABB.Height > _maxSizeAABB.Size.Y)
                        _maxSizeAABB = (i, new Point((int)MathF.Ceiling(i.AABB.Width / 2f), (int)MathF.Ceiling(i.AABB.Height / 2f)), new Point(i.AABB.Width, i.AABB.Height));
            }
        }
        /// <summary>Clear the tree of all objects</summary>
        public static void Clear()
        {
            _mainNode.FreeSubNodes();
            _mainNode.Reset();
            _stored.Clear();
            _maxSizeAABB = (default, Point.Zero, Point.Zero);
        }
        /// <summary>Re-insert <paramref name="item"/> into the tree</summary>
        public static void Update(T item)
        {
            _stored[item].Remove(item);
            var iCenter = item.AABB.Center;
            if (Bounds.Left > iCenter.X || Bounds.Top > iCenter.Y || Bounds.Right < iCenter.X + 1 || Bounds.Bottom < iCenter.Y + 1)
            {
                int newLeft = Math.Min(Bounds.Left, iCenter.X),
                    newTop = Math.Min(Bounds.Top, iCenter.Y),
                    newWidth = Bounds.Right - newLeft,
                    newHeight = Bounds.Bottom - newTop;
                Bounds = new Rectangle(newLeft, newTop, Math.Max(newWidth, iCenter.X - newLeft + 1), Math.Max(newHeight, iCenter.Y - newTop + 1));
            }
            else
                _stored[item] = _mainNode.Add(item);
        }
        public static IEnumerable<T> Query(Rectangle area)
        {
            foreach (var i in _mainNode.Query(new Rectangle(area.X - _maxSizeAABB.HalfSize.X, area.Y - _maxSizeAABB.HalfSize.Y, _maxSizeAABB.Size.X + area.Width, _maxSizeAABB.Size.Y + area.Height), area))
                yield return i;
        }
        public static IEnumerable<T> Query(Vector2 pos)
        {
            foreach (var i in _mainNode.Query(new Rectangle((int)MathF.Round(pos.X - _maxSizeAABB.HalfSize.X), (int)MathF.Round(pos.Y - _maxSizeAABB.HalfSize.Y), _maxSizeAABB.Size.X + 1, _maxSizeAABB.Size
                .Y + 1), new Rectangle((int)MathF.Round(pos.X), (int)MathF.Round(pos.Y), 1, 1)))
                yield return i;
        }

        public class Node : IPoolable
        {
            const int CAPACITY = 8;

            public Rectangle Bounds { get; internal set; }

            public int AllCount
            {
                get
                {
                    int c = _items.Count;
                    if (_nw != null)
                    {
                        c += _ne.AllCount;
                        c += _se.AllCount;
                        c += _sw.AllCount;
                        c += _nw.AllCount;
                    }
                    return c;
                }
            }
            public IEnumerable<T> AllItems
            {
                get
                {
                    foreach (var i in _items)
                        yield return i;
                    if (_nw != null)
                    {
                        foreach (var i in _ne.AllItems)
                            yield return i;
                        foreach (var i in _se.AllItems)
                            yield return i;
                        foreach (var i in _sw.AllItems)
                            yield return i;
                        foreach (var i in _nw.AllItems)
                            yield return i;
                    }
                }
            }
            public IEnumerable<T> AllSubItems
            {
                get
                {
                    foreach (var i in _ne.AllItems)
                        yield return i;
                    foreach (var i in _se.AllItems)
                        yield return i;
                    foreach (var i in _sw.AllItems)
                        yield return i;
                    foreach (var i in _nw.AllItems)
                        yield return i;
                }
            }

            readonly HashSet<T> _items = new HashSet<T>();

            internal Node _parent, _ne, _se, _sw, _nw;

            public Node Add(T item)
            {
                Node Bury(T i, Node n)
                {
                    if (n._ne.Bounds.Contains(i.AABB.Center))
                        return n._ne.Add(i);
                    if (n._se.Bounds.Contains(i.AABB.Center))
                        return n._se.Add(i);
                    if (n._sw.Bounds.Contains(i.AABB.Center))
                        return n._sw.Add(i);
                    if (n._nw.Bounds.Contains(i.AABB.Center))
                        return n._nw.Add(i);
                    return n;
                }
                if (_nw == null)
                    if (_items.Count >= CAPACITY && Bounds.Width * Bounds.Height > 1024)
                    {
                        int halfWidth = Bounds.Width / 2,
                            halfHeight = Bounds.Height / 2;
                        _nw = Pool<Node>.Spawn();
                        _nw.Bounds = new Rectangle(Bounds.Left, Bounds.Top, halfWidth, halfHeight);
                        _nw._parent = this;
                        _sw = Pool<Node>.Spawn();
                        int midY = Bounds.Top + halfHeight,
                            height = Bounds.Bottom - midY;
                        _sw.Bounds = new Rectangle(Bounds.Left, midY, halfWidth, height);
                        _sw._parent = this;
                        _ne = Pool<Node>.Spawn();
                        int midX = Bounds.Left + halfWidth,
                            width = Bounds.Right - midX;
                        _ne.Bounds = new Rectangle(midX, Bounds.Top, width, halfHeight);
                        _ne._parent = this;
                        _se = Pool<Node>.Spawn();
                        _se.Bounds = new Rectangle(midX, midY, width, height);
                        _se._parent = this;
                        foreach (var i in _items)
                            _stored[i] = Bury(i, this);
                        _items.Clear();
                    }
                    else
                        goto add;
                return Bury(item, this);
            add:
                _items.Add(item);
                return this;
            }
            public void Remove(T item)
            {
                _items.Remove(item);
                if (_parent?._nw != null && _parent.AllCount < CAPACITY)
                {
                    foreach (var i in _parent.AllSubItems)
                    {
                        _parent._items.Add(i);
                        _stored[i] = _parent;
                    }
                    Pool<Node>.Free(_parent._ne);
                    Pool<Node>.Free(_parent._se);
                    Pool<Node>.Free(_parent._sw);
                    Pool<Node>.Free(_parent._nw);
                    _parent._ne = null;
                    _parent._se = null;
                    _parent._sw = null;
                    _parent._nw = null;
                }
            }
            public IEnumerable<T> Query(Rectangle broad, Rectangle query)
            {
                if (_nw == null)
                {
                    foreach (T i in _items)
                        if (query.Intersects(i.AABB))
                            yield return i;
                    yield break;
                }
                if (_ne.Bounds.Contains(broad))
                {
                    foreach (var i in _ne.Query(broad, query))
                        yield return i;
                    yield break;
                }
                if (broad.Contains(_ne.Bounds))
                    foreach (var i in _ne.AllItems)
                        yield return i;
                else if (_ne.Bounds.Intersects(broad))
                    foreach (var i in _ne.Query(broad, query))
                        yield return i;
                if (_se.Bounds.Contains(broad))
                {
                    foreach (var i in _se.Query(broad, query))
                        yield return i;
                    yield break;
                }
                if (broad.Contains(_se.Bounds))
                    foreach (var i in _se.AllItems)
                        yield return i;
                else if (_se.Bounds.Intersects(broad))
                    foreach (var i in _se.Query(broad, query))
                        yield return i;
                if (_sw.Bounds.Contains(broad))
                {
                    foreach (var i in _sw.Query(broad, query))
                        yield return i;
                    yield break;
                }
                if (broad.Contains(_sw.Bounds))
                    foreach (var i in _sw.AllItems)
                        yield return i;
                else if (_sw.Bounds.Intersects(broad))
                    foreach (var i in _sw.Query(broad, query))
                        yield return i;
                if (_nw.Bounds.Contains(broad))
                {
                    foreach (var i in _nw.Query(broad, query))
                        yield return i;
                    yield break;
                }
                if (broad.Contains(_nw.Bounds))
                    foreach (var i in _nw.AllItems)
                        yield return i;
                else if (_nw.Bounds.Intersects(broad))
                    foreach (var i in _nw.Query(broad, query))
                        yield return i;
            }

            public void Reset() => _items.Clear();

            internal void FreeSubNodes()
            {
                if (_nw == null)
                    return;
                _ne.FreeSubNodes();
                _se.FreeSubNodes();
                _sw.FreeSubNodes();
                _nw.FreeSubNodes();
                Pool<Node>.Free(_ne);
                Pool<Node>.Free(_se);
                Pool<Node>.Free(_sw);
                Pool<Node>.Free(_nw);
                _ne = null;
                _se = null;
                _sw = null;
                _nw = null;
            }
        }
    }
}