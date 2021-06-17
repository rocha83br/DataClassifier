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
using System.Collections.Generic;

using PTStemmer.Implementations;
using PTStemmer.Helpers.Datastructures;
using System.Collections.Concurrent;

namespace PTStemmer
{
	 /// <summary>
	 /// Abstract class that provides the main features to all the stemmers
	 /// @author Pedro Oliveira
	 /// </summary>
	public abstract class Stemmer : IDisposable
	{
		private bool cacheStems;
		private LRUCache<string, string> lruCache;
		private HashSet<string> toIgnore = new HashSet<string>();

		/// <summary>
		/// Stemmer construction factory
		/// </summary>
		/// <param name="stype">
		/// A <see cref="StemmerType"/>
		/// </param>
		/// <returns>
		/// A <see cref="Stemmer"/>
		/// </returns>
		public static Stemmer StemmerFactory()
		{
            return new PorterStemmer();
		}

		/// <summary>
		/// Create a LRU Cache, caching the last <code>size</code> stems
		/// </summary>
		/// <param name="size">
		/// A <see cref="System.Int32"/>
		/// </param>
		public void EnableCaching()
		{
			cacheStems = true;
			lruCache = new LRUCache<string,string>();
		}

		/// <summary>
		/// Disable and deletes the LRU Cache
		/// </summary>
		public void DisableCaching()
		{
			cacheStems = false;
			lruCache = null;
		}

		/// <summary>
		/// Check if LRU Cache is enabled
		/// </summary>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public bool IsCachingEnabled()
		{
			return cacheStems;
		}

		/// <summary>
		/// Add list of words to ignore list
		/// </summary>
		/// <param name="words">
		/// A <see cref="String"/>
		/// </param>
		public void Ignore(string[] words)
		{
			foreach(string word in words)
				toIgnore.Add(word);
		}
		
		/// <summary>
		/// Add Collection of words to ignore list
		/// </summary>
		/// <param name="words">
		/// A <see cref="ICollection`1"/>
		/// </param>
		public void ignore(ICollection<string> words)
		{
            foreach(var word in words)
			    toIgnore.Add(word);
		}

		/// <summary>
		/// Clear the contents of the ignore list
		/// </summary>
		public void clearIgnoreList()
		{
			toIgnore.Clear();
		}

		/// <summary>
		/// Performs stemming on the <code>phrase</code>, using a simple space tokenizer
		/// </summary>
		/// <param name="phrase">
		/// A <see cref="String"/>
		/// </param>
		/// <returns>
		/// A <see cref="String"/>
		/// </returns>
		private string[] getPhraseStems(string phrase)
		{
            ConcurrentBag<string> result = new ConcurrentBag<string>();
            string[] splitted = phrase.Split(' ');

            splitted.AsParallel().ForAll(splt => {
                result.Add(getWordStem(splt));
            });

            return result.ToArray();
		}

		/// <summary>
		/// Performs stemming on the <code>word</code>
		/// </summary>
		/// <param name="word">
		/// A <see cref="String"/>
		/// </param>
		/// <returns>
		/// A <see cref="String"/>
		/// </returns>
		private string getWordStem(string word)
		{
			string res;
			word = word.Trim().ToLower();

			if(cacheStems)
				if(lruCache.TryGetValue(word,out res))
					return res;

			if(toIgnore.Contains(word))
				return word;
			
			res = Stemming(word);

			if(cacheStems)
				lruCache.Add(word,res);

			return res;
		}

        public abstract string Stemming(string word);

        public void Dispose()
        {
            GC.ReRegisterForFinalize(this);
        }
    }
}
