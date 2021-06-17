/**
 * PTStemmer - A Stemming toolkit for the Portuguese language (C) 2008-2010 Pedro Oliveira
 * Emprovements to parallel objects on .NET 4.5 (C) 2017 Renato Rocha
 * 
 * This file is part of PTStemmer.
 * PTStemmer is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * PTStemmer is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with PTStemmer. If not, see <http://www.gnu.org/licenses/>.
 * 
 */

using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PTStemmer.Helpers.Datastructures
{
    /// <summary>
    /// Object-oriented Suffix Tree implementation
    /// @author Pedro Oliveira
    /// Emprovements to parallel objects on .NET 4.5
    /// @author Renato Rocha
    /// </summary>
    public class SuffixTree<T>
    {
        private SuffixTreeNode<T> root;
        private ConcurrentDictionary<string, int> properties;

        public SuffixTree()
        {
            root = new SuffixTreeNode<T>();
            properties = new ConcurrentDictionary<string, int>();
        }

        public SuffixTree(T val, string[] suffixes) : this()
        {
            suffixes.AsParallel().ForAll(suffix =>
            {
                addSuffix(suffix, val);
            });
        }

        public ConcurrentDictionary<string, int> Properties
        {
            get
            {
                return properties;
            }
            set
            {
                properties = value;
            }
        }

        public bool containsProperty(string property)
        {
            return properties.ContainsKey(property);
        }

        /// <summary>
        /// Add suffix to the Suffix Tree
        /// </summary>
        /// <param name="suffix">
        /// A <see cref="String"/>
        /// </param>
        /// <param name="val">
        /// A <see cref="T"/>
        /// </param>
        public void addSuffix(string suffix, T val)
        {
            SuffixTreeNode<T> node = root;
            char c;
            for (int i = suffix.Length - 1; i >= 0; i--)
            {
                c = suffix[i];
                node = node.addEdge(c);
            }
            node.ValueIsNull = false;
            node.Value = val;
        }

        /// <summary>
        /// Checks if Suffix Tree contains word
        /// </summary>
        /// <param name="word">
        /// A <see cref="String"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// </returns>
        public bool contains(string word)
        {
            SuffixTreeNode<T> cnode = root;
            char c;
            for (int i = word.Length - 1; i >= 0; i--)
            {
                c = word[i];
                cnode = cnode[c];
                if (cnode == null)
                    return false;
            }
            if (cnode != null && !cnode.ValueIsNull)
                return true;
            return false;
        }

        /// <summary>
        /// Get value saved on the longest suffix of the word
        /// </summary>
        /// <param name="word">
        /// A <see cref="String"/>
        /// </param>
        /// <returns>
        /// A <see cref="T"/>
        /// </returns>
        public T getLongestSuffixValue(string word)
        {
            KeyValuePair<string, T>? res = getLongestSuffixAndValue(word);

            return res.HasValue ? res.Value.Value : default(T);
        }

        /// <summary>
        /// Get word's longest suffix present in the tree
        /// </summary>
        /// <param name="word">
        /// A <see cref="String"/>
        /// </param>
        /// <returns>
        /// A <see cref="String"/>
        /// </returns>
        public String getLongestSuffix(string word)
        {
            KeyValuePair<string, T>? res = getLongestSuffixAndValue(word);

            if (res.HasValue)
                return string.Empty;

            return res.HasValue ? res.Value.Key : string.Empty;
        }

        /// <summary>
        /// Get word's longest suffix and value
        /// </summary>
        /// <param name="word">
        /// A <see cref="String"/>
        /// </param>
        /// <returns>
        /// A <see cref="Pair`2"/>
        /// </returns>
        public KeyValuePair<string, T>? getLongestSuffixAndValue(string word)
        {
            SuffixTreeNode<T> cnode = root;
            int longestSuffixIndex = -1;
            T valueToReturn = default(T);
            char c;
            for (int i = word.Length - 1; i >= 0; i--)
            {
                c = word[i];
                cnode = cnode[c];
                if (cnode != null)
                {
                    if (!cnode.ValueIsNull)
                    {
                        longestSuffixIndex = i;
                        valueToReturn = cnode.Value;
                    }
                }
                else
                    break;
            }
            if (longestSuffixIndex != -1)
                return new KeyValuePair<string, T>(word.Substring(longestSuffixIndex), valueToReturn);

            return null;
        }

        /// <summary>
        /// Get all the suffixes in the word and their values
        /// </summary>
        /// <param name="word">
        /// A <see cref="String"/>
        /// </param>
        /// <returns>
        /// A <see cref="List`1"/>
        /// </returns>
        public ConcurrentDictionary<string, T> getLongestSuffixesAndValues(string word)
        {
            char c;
            SuffixTreeNode<T> cnode = root;
            ConcurrentDictionary<string, T> res = new ConcurrentDictionary<string, T>();

            for (int i = word.Length - 1; i >= 0; i--)
            {
                c = word[i];
                cnode = cnode[c];
                if (cnode != null)
                {
                    if (!cnode.ValueIsNull)
                        res.TryAdd(word.Substring(i), cnode.Value);
                }
                else
                    break;
            }
            return res;
        }
    }
}
