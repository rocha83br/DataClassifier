﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.IO.Compression;
using Newtonsoft.Json;
using Rochas.DataClassifier.Extensions;

namespace Rochas.DataClassifier
{
    public static class RochasClassifier
    {
        #region Declarations

        readonly static ConcurrentBag<string> groupList = new ConcurrentBag<string>();
        static Dictionary<string, SortedSet<uint>> searchTree = new Dictionary<string, SortedSet<uint>>();
        readonly static ConcurrentDictionary<string, ConcurrentBag<uint>> hashedTree = new ConcurrentDictionary<string, ConcurrentBag<uint>>();
        readonly static string[] specialChars = { "@", "%", "#", "_", "/", "|", "\\", ";", ":", ".", ",", "*", "(", ")", "[", "]", "+", "-", "=", "\"", "'", "´", "`", "?", "!" };
        readonly static string[] skipWords = new[] {
        "de","a","o","que","e","do","da","em","um","para","é","com","não","uma","os","no","se","na","por","mais","as","dos","como","mas","foi","ao","ele","das",
        "tem","à","seu","sua","ou","ser","quando","muito","há","nos","já","está","eu","também","só","pelo","pela","até","isso","ela","entre","era","depois","sem",
        "mesmo","aos","ter","seus","quem","nas","me","esse","eles","estão","você","tinha","foram","essa","num","nem","suas","meu","às","minha","têm","numa","pelos",
        "elas","havia","seja","qual","será","nós","tenho","lhe","deles","essas","esses","pelas","este","fosse","dele","tu","te","vocês","vos","lhes","meus","minhas",
        "teu","tua","teus","tuas","nosso","nossa","nossos","nossas","dela","delas","esta","estes","estas","aquele","aquela","aqueles","aquelas","isto","aquilo","estou",
        "está","estamos","estão","estive","esteve","estivemos","estiveram","estava","estávamos","estavam","estivera","estivéramos","esteja","estejamos","estejam","estivesse",
        "estivéssemos","estivessem","estiver","estivermos","estiverem","hei","há","havemos","hão","houve","houvemos","houveram","houvera","houvéramos","haja","hajamos","hajam",
        "houvesse","houvéssemos","houvessem","houver","houvermos","houverem","houverei","houverá","houveremos","houverão","houveria","houveríamos","houveriam","sou","somos","são",
        "era","éramos","eram","fui","foi","fomos","foram","fora","fôramos","seja","sejamos","sejam","fosse","fôssemos","fossem","for","formos","forem","serei","será","seremos",
        "serão","seria","seríamos","seriam","tenho","tem","temos","tém","tinha","tínhamos","tinham","tive","teve","tivemos","tiveram","tivera","tivéramos","tenha","tenhamos",
        "tenham","tivesse","tivéssemos","tivessem","tiver","tivermos","tiverem","terei","terá","teremos","terão","teria","teríamos","teriam" };

        #endregion

        #region Public Methods

        public static void Init(IEnumerable<string> groups)
        {
            if ((groups == null) || (groups.Count() == 0))
                throw new ArgumentNullException("groups");

            groups.AsParallel().ForAll(group =>
            {
                AddGroup(group);
            });
        }

        public static void Init(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException("filePath");

            var fileContent = File.OpenText(filePath);
            var groups = new List<string>();

            while (!fileContent.EndOfStream)
            {
                var group = fileContent.ReadLine();

                groups.Add(group);
            }

            Init(groups);
        }

        public static void AddGroup(string group)
        {
            group = group.Trim();

            if ((group.Length > 2) || !group.ToLower().Equals("null"))
            {
                var filteredGroup = filterSpecialChars(group.Trim().ToLower());
                var upperGroup = filteredGroup.ToUpper();
                var titledGroup = filteredGroup.ToTitleCase();

                if (!groupList.Contains(group))
                    groupList.Add(group);

                if (!groupList.Contains(filteredGroup))
                    groupList.Add(filteredGroup);

                if (!groupList.Contains(upperGroup))
                    groupList.Add(upperGroup);

                if (!groupList.Contains(titledGroup))
                    groupList.Add(titledGroup);
            }
        }

        public static bool RemoveGroup(string group)
        {
            var result = false;
            var filteredGroup = filterSpecialChars(group.Trim().ToLower());
            var upperGroup = filteredGroup.ToUpper();
            var titledGroup = filteredGroup.ToTitleCase();

            if (!groupList.Contains(group))
                result = groupList.TryTake(out group);

            if (!groupList.Contains(filteredGroup))
                result = groupList.TryTake(out filteredGroup);

            if (!groupList.Contains(upperGroup))
                result = groupList.TryTake(out upperGroup);

            if (!groupList.Contains(titledGroup))
                result = groupList.TryTake(out titledGroup);

            return result;
        }

        public static void Train(string group, string text)
        {
            ConcurrentBag<uint> hashedWordList = null;

            if (!hashedTree.ContainsKey(group))
                hashedWordList = new ConcurrentBag<uint>();
            else
                hashedWordList = hashedTree[group];

            foreach (var word in filterSpecialChars(text).Trim().Split(' '))
                stemmHash(word, hashedWordList);

            if (!hashedTree.ContainsKey(group))
                hashedTree.TryAdd(group, hashedWordList);
        }

        public static void Train(string group, IEnumerable<string> mappedList)
        {
            foreach (var text in mappedList)
            {
                try
                {
                    Train(group, text);
                }
                catch (Exception ex)
                {
                    registerException(ex.InnerException ?? ex);
                    throw ex;
                }
            }
        }

        public static void Train(IEnumerable<string> rawList)
        {
            var reduceList = new ConcurrentBag<string>(rawList);

            mapReduce(reduceList);

            prepareSearchTree();
        }

        public static void TrainFromFile(string filePath, int page = 0, int size = 0)
        {
            var result = new ConcurrentBag<string>();
            var fileContent = File.OpenText(filePath);

            int itemsCount = 0;
            while (!fileContent.EndOfStream)
            {
                var offset = (page * size);

                if (((page == 0) && (itemsCount < size))
                    || ((page > 0) && (itemsCount > offset)))
                {
                    var lineContent = fileContent.ReadLine();
                    result.Add(lineContent);
                }

                if (result.Count >= size)
                    break;
                else
                    itemsCount++;
            }

            Train(result);
        }

        public static string SaveTrainingData()
        {
            var content = JsonConvert.SerializeObject(hashedTree);
            return compressText(content);
        }

        public static void SaveTrainingData(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException("filePath");

            var compressedContent = SaveTrainingData();

            File.WriteAllText(filePath, compressedContent);
        }

        public static void FromTrainingData(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException("filePath");

            var compressedContent = File.OpenText(filePath);
            var content = uncompressText(compressedContent.ReadToEnd());

            searchTree = JsonConvert.DeserializeObject<Dictionary<string, SortedSet<uint>>>(content);
        }

        public static IDictionary<string, int> Classify(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentNullException("text");

            var hashedWordList = new ConcurrentBag<uint>();
            filterSpecialChars(text).Trim().ToLower().Split(' ').AsParallel().ForAll(word =>
            {
                stemmHash(word, hashedWordList);
            });

            var result = setGroupScore(hashedWordList);

            var orderedResult = result.OrderByDescending(res => res.Value);

            var scoreResult = setScorePercent(orderedResult);

            return scoreResult;
        }

        public static void Flush()
        {
            hashedTree.Clear();
        }

        #endregion

        #region Helper Methods

        private static string filterSpecialChars(string value)
        {
            foreach (var character in specialChars)
                value = value.Replace(character, " ");

            return value;
        }

        private static void stemmHash(string word, ConcurrentBag<uint> hashedWordList)
        {
            var trimmedWord = word.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedWord) && !skipWords.Contains(trimmedWord))
            {
                using (var stemmer = PTStemmer.Stemmer.StemmerFactory())
                {
                    uint hashedWord = 0;
                    stemmer.DisableCaching();

                    hashedWord = stemmer.Stemming(trimmedWord.ToLower()).GetCustomHashCode();                    

                    if (!hashedWordList.Contains(hashedWord))
                        hashedWordList.Add(hashedWord);
                }
            }
        }

        private static void prepareSearchTree()
        {
            foreach (var item in hashedTree)
            {
                searchTree.Add(item.Key, new SortedSet<uint>());

                foreach (var itemValue in item.Value)
                    searchTree[item.Key].Add(itemValue);
            }
        }

        private static void mapReduce(ConcurrentBag<string> reduceList)
        {
            var startTime = DateTime.Now;
            Console.WriteLine("Start...");
            Console.WriteLine();

            int fullCount = 0;
            groupList.OrderByDescending(grp => grp.Length).AsParallel().AsOrdered().ForAll(group =>
            {
                var processPercent = ((fullCount++ * 100) / groupList.Count()) + 1;

                Console.WriteLine(string.Format("- ({1}% Elapsed) Training data from {0} group...", group, processPercent));

                var map = new ConcurrentBag<string>();
                foreach (var text in reduceList)
                {
                    var remText = text;
                    if (text.Contains(group))
                    {
                        map.Add(text);
                        reduceList.TryTake(out remText);
                    }
                }

                Train(group.ToLower(), map);
            });

            var lastElapsedMinutes = Math.Round((DateTime.Now - startTime).TotalMinutes, 0);

            Console.WriteLine();
            Console.WriteLine(string.Format("Finished in {0} minutes.", lastElapsedMinutes));
        }

        private static ConcurrentDictionary<string, int> setGroupScore(ConcurrentBag<uint> hashedWordList)
        {
            var result = new ConcurrentDictionary<string, int>();

            searchTree.AsParallel().ForAll(item =>
            {
                var score = 0;
                foreach (var hashedWord in item.Value)
                    if (hashedWordList.Contains(hashedWord))
                        score += 1;

                if (score > 0)
                {
                    if (!result.ContainsKey(item.Key))
                        result.TryAdd(item.Key, score);
                    else
                        result[item.Key] += score;
                }
            });

            return result;
        }

        private static Dictionary<string, int> setScorePercent(IOrderedEnumerable<KeyValuePair<string, int>> groupScore)
        {
            var result = new Dictionary<string, int>();

            int maxScore = groupScore.Sum(grp => grp.Value);

            foreach (var group in groupScore)
            {
                var percent = ((group.Value * 100) / maxScore);

                if (percent > 0)
                    result.Add(group.Key, percent);
            }

            return result;
        }

        private static byte[] compressBinary(byte[] rawSource)
        {
            var memDestination = new MemoryStream();
            var memSource = new MemoryStream(rawSource);
            using (var gzipStream = new GZipStream(memDestination, CompressionMode.Compress))
            {
                memSource.CopyTo(gzipStream);

                return memDestination.ToArray();
            }
        }

        private static byte[] uncompressBinary(byte[] compressedSource)
        {
            byte[] unpackedContent = new byte[compressedSource.Length * 20];
            var memSource = new MemoryStream(compressedSource);

            var gzipStream = new GZipStream(memSource, CompressionMode.Decompress);

            var readedBytes = gzipStream.Read(unpackedContent, 0, unpackedContent.Length);

            var memDestination = new MemoryStream(unpackedContent, 0, readedBytes);

            return memDestination.ToArray();
        }

        private static string compressText(string rawText)
        {
            byte[] rawBinary = null;
            byte[] compressedBinary = null;

            rawBinary = ASCIIEncoding.ASCII.GetBytes(rawText);

            compressedBinary = compressBinary(rawBinary);

            return Convert.ToBase64String(compressedBinary);
        }

        private static string uncompressText(string compressedText)
        {
            string result = string.Empty;
            byte[] compressedBinary = Convert.FromBase64String(compressedText);
            byte[] destinBinary = uncompressBinary(compressedBinary);

            result = new string(ASCIIEncoding.ASCII.GetChars(destinBinary));

            return result.ToString();
        }

        private static void registerException(Exception ex)
        {
            var serialEx = string.Format("Error: {0}{1}{2}Trace: {3}", ex.Message, Environment.NewLine, Environment.NewLine, ex.StackTrace);

            var serialFullPath = string.Concat("RochasClassifier_ExceptionLog_", DateTime.Now.ToString("yyyyMMdd_HHmm_fff"), ".log");

            File.WriteAllText(serialFullPath, serialEx);
        }

        #endregion
    }
}