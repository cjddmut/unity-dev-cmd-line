/*
 * Created by C.J. Kimberlin (http://cjkimberlin.com)
 * 
 * The MIT License (MIT)
 * 
 * Copyright (c) 2023
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 *
 */

using System.Collections.Generic;

public class Trie
{
    public readonly Node root;

    public class Node
    {
        public readonly char value;
        public readonly int depth;

        public bool isCompleteString;

        // PERF: If we have a small set of characters, could be an array look up
        private Dictionary<char, Node> _childrenToNode = new Dictionary<char, Node>();
        private List<Node> _children = new List<Node>();

        public int childrenCount => _childrenToNode.Count;

        public Node(char value, int depth)
        {
            this.value = value;
            this.depth = depth;
        }

        public Node GetChild(char c, bool caseInsensitive)
        {
            if (caseInsensitive)
            {
                if (_childrenToNode.TryGetValue(char.ToLower(c), out Node node))
                {
                    return node;
                }

                if (_childrenToNode.TryGetValue(char.ToUpper(c), out node))
                {
                    return node;
                }
            }
            else
            {
                if (_childrenToNode.TryGetValue(c, out Node node))
                {
                    return node;
                }
            }

            return null;
        }

        public void AddChild(Node node)
        {
            if (!_childrenToNode.ContainsKey(node.value))
            {
                _childrenToNode[node.value] = node;
                _children.Add(node);
            }
        }

        public Node GetFirstChild()
        {
            return _children.Count > 0 ? _children[0] : null;
        }

        public Node[] GetChildren()
        {
            return _children.ToArray();
        }
    }

    public Trie()
    {
        root = new Node(default, 0);
    }

    public Node Prefix(string s, bool caseInsensitive = false)
    {
        Node currentNode = root;
        Node result = currentNode;

        foreach (char c in s)
        {
            currentNode = currentNode.GetChild(c, caseInsensitive);

            if (currentNode == null)
            {
                break;
            }

            result = currentNode;
        }

        return result;
    }

    public bool Search(string s, bool caseInsensitive = false)
    {
        Node prefix = Prefix(s, caseInsensitive);
        return prefix.depth == s.Length && prefix.isCompleteString;
    }

    public void Add(IEnumerable<string> items)
    {
        foreach (string item in items)
        {
            Add(item);
        }
    }

    public void Add(string[] items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            Add(items[i]);
        }
    }

    public void Add(string s)
    {
        Node prefix = Prefix(s);
        Node current = prefix;

        for (int i = current.depth; i < s.Length; i++)
        {
            Node newNode = new Node(s[i], current.depth + 1);
            current.AddChild(newNode);
            current = newNode;
        }

        current.isCompleteString = true;
    }
}