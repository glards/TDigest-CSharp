using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace TDigest
{
    public class RBTree<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private const int Black = 0;
        private const int Red = 1;

        private const int Left = 0;
        private const int Right = 1;

        private readonly IComparer<TKey> comparer;

        private class Node<TKey, TValue>
        {
            public Node<TKey, TValue>[] link;

            public Node<TKey, TValue> left
            {
                get { return link[Left]; }
                set { link[Left] = value; }
            }

            public Node<TKey, TValue> right
            {
                get { return link[Right]; }
                set { link[Right] = value; }
            }
            
            public int color;

            public TKey key;
            public TValue value;

            public Node() : this(default(TKey), default(TValue))
            {
            }

            public Node(TKey key, TValue value)
            {
                this.key = key;
                this.value = value;
                this.color = Red;
                this.link = new Node<TKey, TValue>[2];
                this.left = null;
                this.right = null;
            }
        }

        private static bool IsRed(Node<TKey, TValue> n)
        {
            return n != null && n.color == Red;
        }

        private static Node<TKey, TValue> RotateSingle(Node<TKey, TValue> node, int dir)
        {
            Node<TKey, TValue> save = node.link[dir == 0 ? 1 : 0];
            node.link[dir == 0 ? 1 : 0] = save.link[dir];
            save.link[dir] = node;

            node.color = Red;
            save.color = Black;

            return save;
        }

        private static Node<TKey, TValue> RotateDouble(Node<TKey, TValue> node, int dir)
        {
            node.link[dir == 0 ? 1 : 0] = RotateSingle(node.link[dir == 0 ? 1 : 0], dir == 0 ? 1 : 0);
            return RotateSingle(node, dir);
        }

        private static void SwapData(Node<TKey, TValue> a, Node<TKey, TValue> b)
        {
            TKey tmp = a.key;
            a.key = b.key;
            b.key = tmp;

            TValue tmp2 = a.value;
            a.value = b.value;
            b.value = tmp2;
        }

        private static void DeleteNode(Node<TKey, TValue> node)
        {
            if (node == null) return;
            node.left = null;
            node.right = null;
        }

        private Node<TKey, TValue> root = null;

        public RBTree() : this(Comparer<TKey>.Default)
        {
        }

        public RBTree(IComparer<TKey> comparer)
        {
            this.comparer = comparer;
        }

        public int Count { get; private set; }

        public bool IsEmpty()
        {
            return Count == 0;
        }

        /// <summary>
        /// Inserts or update a key-value in the tree.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>True if the key was inserted; False if it was updated.</returns>
        public bool Put(TKey key, TValue value)
        {
            if (root == null)
            {
                root = new Node<TKey, TValue>(key, value);
                root.color = Black;
                Count = 1;
                return true;
            }

            Node<TKey, TValue> head = new Node<TKey, TValue>();
            Node<TKey, TValue> g, t, p, q;

            t = head;
            g = null;
            p = null;
            t.right = root;
            t.left = null;
            q = t.right;

            bool new_node = false;
            int dir = Left;
            int last = 0;

            while (true)
            {
                if (q == null)
                {
                    q = new Node<TKey, TValue>(key, value);
                    Count++;
                    p.link[dir] = q;
                    new_node = true;
                }
                else if (IsRed(q.left) && IsRed(q.right))
                {
                    q.color = Red;
                    q.left.color = Black;
                    q.right.color = Black;
                }

                if (IsRed(q) && IsRed(p))
                {
                    int dir2 = t.right == g ? 1 : 0;

                    if (q == p.link[last])
                    {
                        t.link[dir2] = RotateSingle(g, last == 0 ? 1 : 0);
                    }
                    else
                    {
                        t.link[dir2] = RotateDouble(g, last == 0 ? 1 : 0);
                    }
                }

                if (new_node)
                {
                    break;
                }

                int result = comparer.Compare(q.key, key);
                if (result == 0)
                {
                    q.value = value;
                    return false;
                }

                last = dir;
                dir = result < 0 ? 1 : 0;

                if (g != null)
                {
                    t = g;
                }

                g = p;
                p = q;
                q = q.link[dir];
            }

            root = head.right;
            root.color = Black;
            return true;
        }

        /// <summary>
        /// Deletes a key-value from the tree.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The deleted key-value.</returns>
        public KeyValuePair<TKey, TValue>? Delete(TKey key)
        {
            KeyValuePair<TKey, TValue>? ret = null;
            Node<TKey, TValue> head = new Node<TKey, TValue>();
            Node<TKey, TValue> q, p, g;
            Node<TKey, TValue> f = null;
            int dir = Right;

            if (root == null) return ret;

            g = null;
            p = null;
            q = head;
            q.right = root;

            while (q.link[dir] != null)
            {
                int cmp;
                int last = dir;
                g = p;
                p = q;
                q = q.link[dir];

                cmp = comparer.Compare(q.key, key);
                dir = cmp < 0 ? 1 : 0;

                if (cmp == 0)
                {
                    f = q;
                }

                if (!IsRed(q) && !IsRed(q.link[dir]))
                {
                    if (IsRed((q.link[dir == 0 ? 1 : 0])))
                    {
                        p = p.link[last] = RotateSingle(q, dir);
                    }
                    else
                    {
                        Node<TKey, TValue> s = p.link[last == 0 ? 1 : 0];

                        if (s != null)
                        {
                            if (!IsRed(s.link[last == 0 ? 1 : 0]) && !IsRed(s.link[last]))
                            {
                                p.color = Black;
                                s.color = Red;
                                q.color = Red;
                            }
                            else
                            {
                                int dir2 = g.right == p ? 1 : 0;

                                if (IsRed(s.link[last]))
                                {
                                    g.link[dir2] = RotateDouble(p, last);
                                }
                                else if (IsRed(s.link[last == 0 ? 1 : 0]))
                                {
                                    g.link[dir2] = RotateSingle(p, last);
                                }

                                g.color = Red;
                                g.link[dir2].color = Red;
                                g.link[dir2].left.color = Black;
                                g.link[dir2].right.color = Red;
                            }
                        }
                    }
                }
            }

            if (f != null)
            {
                Count--;
                ret = new KeyValuePair<TKey, TValue>(f.key, f.value);
                SwapData(f, q);
                p.link[p.right == q ? 1 : 0] = q.link[q.left == null ? 1 : 0];
                DeleteNode(q);
            }

            root = head.right;
            if (root != null)
            {
                root.color = Black;
            }

            return ret;
        }

        private Node<TKey, TValue> FindNode(TKey key)
        {
            Node<TKey, TValue> node = root;
            int cmp;
            while (node != null)
            {
                cmp = comparer.Compare(key, node.key);
                if (cmp == 0)
                {
                    return node;
                }

                node = node.link[cmp > 0 ? 1 : 0];
            }
            return null;
        }

        public TValue Get(TKey key)
        {
            Node<TKey, TValue> node = FindNode(key);
            if (node != null)
            {
                return node.value;
            }

            return default(TValue);
        }

        public bool Contains(TKey key)
        {
            return FindNode(key) != null;
        }

        /// <summary>
        /// Returns the largest item in the tree.
        /// </summary>
        /// <returns>The largest item in the tree.</returns>
        public KeyValuePair<TKey, TValue>? Max()
        {
            Node<TKey, TValue> node = root;
            if (node == null) return null;
            while (node.right != null)
                node = node.right;
            return new KeyValuePair<TKey, TValue>(node.key, node.value);
        }

        /// <summary>
        /// Returns the smallest item in the tree.
        /// </summary>
        /// <returns>The smallest item in the tree.</returns>
        public KeyValuePair<TKey, TValue>? Min()
        {
            Node<TKey, TValue> node = root;
            if (node == null) return null;
            while (node.left != null)
                node = node.left;
            return new KeyValuePair<TKey, TValue>(node.key, node.value);
        }

        /// <summary>
        /// Returns the largest key in the tree less than or equal to key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>The largest key in the tree less than or equal to key.</returns>
        public KeyValuePair<TKey, TValue>? Floor(TKey key)
        {
            Node<TKey, TValue> node = Floor(root, key);
            if (node == null) return null;
            return new KeyValuePair<TKey, TValue>(node.key, node.value);
        }

        private Node<TKey, TValue> Floor(Node<TKey, TValue> node, TKey key)
        {
            if (node == null) return null;
            int cmp = comparer.Compare(key, node.key);

            if (cmp == 0) return node;

            if (cmp < 0) return Floor(node.left, key);

            Node<TKey, TValue> t = Floor(node.right, key);
            if (t != null) return t;
            return node;
        }

        /// <summary>
        /// Returns the smallest key in the tree greater than or equal to key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>The smallest key in the tree greater than or equal to key</returns>
        public KeyValuePair<TKey, TValue>? Ceiling(TKey key)
        {
            Node<TKey, TValue> node = Ceiling(root, key);
            if (node == null) return null;
            return new KeyValuePair<TKey, TValue>(node.key, node.value);
        }

        private Node<TKey, TValue> Ceiling(Node<TKey, TValue> node, TKey key)
        {
            if (node == null) return null;
            int cmp = comparer.Compare(key, node.key);
            if (cmp == 0) return node;

            if (cmp < 0)
            {
                Node<TKey, TValue> t = Floor(node.left, key);
                if (t != null) return t;
                return node;
            }

            return Ceiling(node.right, key);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            Stack<Node<TKey,TValue>> stack = new Stack<Node<TKey, TValue>>();
            stack.Push(root);
            while (stack.Count != 0)
            {
                Node<TKey, TValue> current = stack.Pop();
                
                if (current.right != null) stack.Push(current.right);
                if (current.left != null)
                {
                    stack.Push(current.left);
                }
                else
                {
                    yield return new KeyValuePair<TKey, TValue>(current.key, current.value);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
