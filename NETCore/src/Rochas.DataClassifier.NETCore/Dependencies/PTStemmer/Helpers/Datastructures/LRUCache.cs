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
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PTStemmer.Helpers.Datastructures
{
	/// <summary>
	/// Simple Least Recently Used (LRU) Cache implementation
	/// </summary>
	public class LRUCache<K,V>
	{
		private int capacity;
		private ConcurrentDictionary<K,V> cache;
		private LinkedList<K> lru;
		
		public LRUCache()
		{
			this.cache = new ConcurrentDictionary<K,V>();
			this.lru = new LinkedList<K>();
		}
		
		public void Add(K key, V val)
		{
			if(cache.ContainsKey(key))
				lru.Remove(key);
			else
			{
				if(lru.Count == capacity)
				{
                    V fake;
					cache.TryRemove(lru.Last.Value, out fake);
					lru.RemoveLast();
                    fake = default(V);
				}
			}	
			cache[key] = val;
			lru.AddFirst(key);
		}
		
		public bool TryGetValue(K key, out V val)
		{
			if(cache.TryGetValue(key, out val))
			{
				lru.Remove(key);
				lru.AddFirst(key);
				return true;
			}
			return false;
		}	
	}		
}
