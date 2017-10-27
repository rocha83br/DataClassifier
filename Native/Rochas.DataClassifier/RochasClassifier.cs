using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Rochas.SoundEx;
using Rochas.DataClassifier.Extensions;
using Rochas.DataClassifier.Enumerators;
using Rochas.DataClassifier.Helpers;
using Rochas.DataClassifier.Models;
using Rochas.DataClassifier.Repositories;

namespace Rochas.DataClassifier
{
    public class RochasClassifier : IDisposable
    {
        #region Declarations

        bool allowHashRepetition;
        bool useSpecialCharsFilter;
        bool useSensitiveCase;
        PhoneticMatchType phoneticType;
        string groupContentSeparator;

        readonly static ConcurrentBag<string> groupList = new ConcurrentBag<string>();
        static Dictionary<string, SortedSet<ulong>> searchTree = new Dictionary<string, SortedSet<ulong>>();
        readonly static ConcurrentDictionary<string, ConcurrentBag<ulong>> hashedTree = new ConcurrentDictionary<string, ConcurrentBag<ulong>>();

        readonly static string languageChars = "àáãçéíóõúÀÁÃÇÉÍÓÕÚ";
        readonly static string cleanLanguageChars = "aaaceioouAAACEIOOU";

        readonly static string[] specialChars = { "@", "%", "#", "/", "\\", ";", ":", ".", ",", "*", "(", ")", "[", "]", "<", ">", "+", "-", "\"", "'", "´", "`", "?", "!" };

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

        #region Constructors

        public RochasClassifier(bool allowRepeat = true, bool filterChars = false, bool sensitiveCase = false, PhoneticMatchType phoneticMatchType = PhoneticMatchType.None, string groupSeparator = "")
        {
            allowHashRepetition = allowRepeat;
            useSpecialCharsFilter = filterChars;
            useSensitiveCase = sensitiveCase;
            phoneticType = phoneticMatchType;
            groupContentSeparator = groupSeparator;
        }

        #endregion

        #region Public Methods

        public void Init(IEnumerable<string> groups)
        {
            if ((groups == null) || (groups.Count() == 0))
                throw new ArgumentNullException("groups");

            groups.AsParallel().ForAll(name =>
            {
                AddGroup(name);
            });

            groups = null;
        }

        public void Init(string filePath, int page = 0, int size = 0)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException("filePath");

            var fileContent = File.OpenText(filePath);
            var tempGroups = new List<string>();

            int itemsCount = 0;
            var offset = (page * size);
            while (!fileContent.EndOfStream)
            {
                var group = fileContent.ReadLine();

                if (((page == 0) && (itemsCount < size))
                    || ((page > 0) && (itemsCount > offset)))
                {
                    if (!string.IsNullOrWhiteSpace(groupContentSeparator))
                        group = group.Substring(0, group.IndexOf(groupContentSeparator));

                    tempGroups.Add(group);
                }

                if (tempGroups.Count >= size)
                    break;
                else
                    itemsCount++;
            }

            Console.WriteLine("Ordering groups to map and reduce...");
            Console.WriteLine();
            Init(tempGroups.Distinct());
        }

        public void AddGroup(string name)
        {
            name = name.Trim();

            if (!name.ToLower().Equals("null"))
            {
                if (useSpecialCharsFilter)
                    name = filterSpecialChars(name);

                if (!groupList.Contains(name))
                    groupList.Add(name);

                if (useSensitiveCase)
                {
                    var lowerGroup = name.ToLower();
                    var upperGroup = name.ToUpper();
                    var titledGroup = name.ToTitleCase();

                    if (!groupList.Contains(lowerGroup))
                        groupList.Add(lowerGroup);

                    if (!groupList.Contains(upperGroup))
                        groupList.Add(upperGroup);

                    if (!groupList.Contains(titledGroup))
                        groupList.Add(titledGroup);
                }
            }
        }

        public bool RemoveGroup(string group)
        {
            var result = false;
            group = group.Trim();

            if (useSpecialCharsFilter)
                group = filterSpecialChars(group);

            if (groupList.Contains(group))
                result = groupList.TryTake(out group);

            if (useSensitiveCase)
            {
                var lowerGroup = group.ToLower();
                var upperGroup = group.ToUpper();
                var titledGroup = group.ToTitleCase();

                if (groupList.Contains(lowerGroup))
                    result = groupList.TryTake(out lowerGroup);

                if (groupList.Contains(upperGroup))
                    result = groupList.TryTake(out upperGroup);

                if (groupList.Contains(titledGroup))
                    result = groupList.TryTake(out titledGroup);
            }

            return result;
        }

        public void Train(string group, string text)
        {
            ConcurrentBag<ulong> hashedWordList = null;

            if (!hashedTree.ContainsKey(group))
                hashedWordList = new ConcurrentBag<ulong>();
            else
                hashedWordList = hashedTree[group];

            if (useSpecialCharsFilter)
                text = filterSpecialChars(text);

            foreach (var word in text.Trim().Split(' '))
                stemmHash(word, hashedWordList);

            if (!hashedTree.ContainsKey(group))
                hashedTree.TryAdd(group, hashedWordList);
        }

        public void Train(string group, IEnumerable<string> mappedList)
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

        public void Train(IEnumerable<string> rawList)
        {
            var reduceList = new ConcurrentBag<string>(rawList);

            mapReduce(reduceList);

            prepareSearchTree();
        }

        public void TrainFromStream(StreamReader streamReader, int page = 0, int size = 0)
        {
            if (groupList.IsEmpty)
                throw new Exception("No groups defined");

            var result = new ConcurrentBag<string>();

            int itemsCount = 0;
            while (!streamReader.EndOfStream)
            {
                var offset = (page * size);

                var lineContent = streamReader.ReadLine();

                if (((page == 0) && (itemsCount < size))
                    || ((page > 0) && (itemsCount > offset)))
                {
                    result.Add(lineContent);
                }

                if (result.Count >= size)
                    break;
                else
                    itemsCount++;
            }

            Train(result);
        }

        public void TrainFromFile(string filePath, int page = 0, int size = 0)
        {
            if (groupList.IsEmpty)
                throw new Exception("No groups defined");

            var result = new ConcurrentBag<string>();
            var fileContent = File.OpenText(filePath);

            TrainFromStream(fileContent, page, size);
        }

        public string SaveTrainingData()
        {
            var content = JsonConvert.SerializeObject(hashedTree);
            return Compressor.CompressText(content);
        }

        public void SaveTrainingData(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException("filePath");

            var compressedContent = SaveTrainingData();

            File.WriteAllText(filePath, compressedContent);
        }

        public void FromTrainingData(string filePath, string connectionString = "")
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException("filePath");

            var compressedContent = File.OpenText(filePath);
            var content = Compressor.UncompressText(compressedContent.ReadToEnd());

            searchTree = JsonConvert.DeserializeObject<Dictionary<string, SortedSet<ulong>>>(content);

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                persistKnowledgeDB(connectionString);

                searchTree.Clear();
                searchTree = null;
            }
        }

        public IDictionary<string, ulong> Classify(string text, int limit = 0, bool matchStop = true, string connectionString = "")
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentNullException("text");

            var hashedWordList = new ConcurrentBag<ulong>();

            if (useSpecialCharsFilter)
                text = filterSpecialChars(text);

            text.Trim().ToLower().Split(' ').AsParallel().ForAll(word =>
            {
                stemmHash(word, hashedWordList);
            });

            IDictionary<string, ulong> result = null;
            if (string.IsNullOrWhiteSpace(connectionString))
                result = setGroupScore(hashedWordList, matchStop);
            else
                result = setDBGroupScore(hashedWordList, matchStop, connectionString);

            var orderedResult = result.OrderByDescending(res => res.Value);

            var scoreResult = setScorePercent(orderedResult, limit);

            return scoreResult;
        }

        public void Flush()
        {
            hashedTree.Clear();
        }

        public void Dispose()
        {
            GC.ReRegisterForFinalize(this);
        }

        #endregion

        #region Helper Methods

        private static string filterLanguageChars(string value)
        {
            int charCount = 0;
            foreach (var character in languageChars)
                value = value.Replace(character, cleanLanguageChars[charCount++]);

            return value;
        }

        private static string filterSpecialChars(string value)
        {
            foreach (var character in specialChars)
                value = value.Replace(character, string.Empty);

            return value;
        }

        private void stemmHash(string word, ConcurrentBag<ulong> hashedWordList)
        {
            var trimmedWord = word.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedWord) && !skipWords.Contains(trimmedWord))
            {
                using (var stemmer = PTStemmer.Stemmer.StemmerFactory())
                {
                    string treatedWord = string.Empty;
                    ulong hashedWord = 0;

                    stemmer.DisableCaching();
                    treatedWord = stemmer.Stemming(trimmedWord.ToLower());

                    if (phoneticType != PhoneticMatchType.None)
                        treatedWord = filterLanguageChars(treatedWord);

                    hashedWord = treatedWord.GetCustomHashCode();

                    if (allowHashRepetition)
                        hashedWordList.Add(hashedWord);
                    else
                    {
                        if (!hashedWordList.Contains(hashedWord))
                            hashedWordList.Add(hashedWord);
                    }

                    if (phoneticType == PhoneticMatchType.UseSondexAlgorithm)
                        hashedWordList.Add(RochasSoundEx.Generate(treatedWord).GetCustomHashCode());
                }
            }
        }

        private static void prepareSearchTree()
        {
            foreach (var item in hashedTree)
            {
                searchTree.Add(item.Key, new SortedSet<ulong>());

                foreach (var itemValue in item.Value)
                    searchTree[item.Key].Add(itemValue);
            }
        }

        private void mapReduce(ConcurrentBag<string> reduceList)
        {
            var startTime = DateTime.Now;
            Console.WriteLine("Start training...");
            Console.WriteLine();

            int fullCount = 0;

            IEnumerable<string> groupOrderedReduceList = null;

            Console.WriteLine("Ordering data to map and reduce...");
            Console.WriteLine();
            if (!string.IsNullOrWhiteSpace(groupContentSeparator))
                groupOrderedReduceList = reduceList.OrderByDescending(res => res.Substring(0, res.IndexOf(groupContentSeparator)).Length)
                                                   .AsParallel().AsOrdered().ToList();
            else
                groupOrderedReduceList = reduceList.OrderByDescending(res => res.Length).AsParallel().AsOrdered().ToList();

            groupList.OrderByDescending(grp => grp.Length).AsParallel().AsOrdered().ForAll(group =>
            {
                var processPercent = ((fullCount++ * 100) / groupList.Count()) + 1;

                Console.WriteLine(string.Format("- ({1}% Elapsed) Training data from {0} group...", group, processPercent));

                var map = new ConcurrentBag<string>();

                foreach (var text in groupOrderedReduceList)
                {
                    var remText = text;
                    if (text.Contains(group))
                    {
                        map.Add(text);
                        reduceList.TryTake(out remText);
                    }
                }

                Train(group, map);
            });

            var lastElapsedMinutes = Math.Round((DateTime.Now - startTime).TotalMinutes, 0);

            Console.WriteLine();
            Console.WriteLine(string.Format("Finished in {0} minutes.", lastElapsedMinutes));
        }

        private bool persistKnowledgeDB(string connectionString)
        {
            if (searchTree.Count == 0)
                throw new Exception("Training data not loaded");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception("ConnectionString not informed");

            var startTime = DateTime.Now;
            Console.WriteLine();
            Console.WriteLine("Start MemoryDB persistence...");
            Console.WriteLine();

            int fullCount = 0;
            KnowledgeRepository.Init(connectionString);
            foreach (var treeItem in searchTree)
            {
                var persistItem = new KnowledgeGroup(treeItem.Key);
                persistItem.Hashes = treeItem.Value.AsParallel().Select(itm => new KnowledgeHash(int.Parse(itm.ToString()))).ToList();

                try
                {
                    var processPercent = ((fullCount++ * 100) / searchTree.Keys.Count()) + 1;
                    Console.WriteLine(string.Format("- ({1}% Elapsed) Persisting data from {0} group...", treeItem.Key, processPercent));

                    KnowledgeRepository.Save(persistItem);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            var lastElapsedMinutes = Math.Round((DateTime.Now - startTime).TotalMinutes, 0);

            Console.WriteLine();
            Console.WriteLine(string.Format("Finished in {0} minutes.", lastElapsedMinutes));

            return true;
        }

        private static IDictionary<string, ulong> setGroupScore(ConcurrentBag<ulong> hashedWordList, bool matchStop)
        {
            var result = new ConcurrentDictionary<string, ulong>();
            SortedSet<ulong> userHashedWords = new SortedSet<ulong>(hashedWordList.ToList());

            try
            {
                searchTree.Where(itm => itm.Value.Any(itmv => userHashedWords.Contains(itmv))).AsParallel().ForAll(item =>
                {
                    uint score = 0;
                    var distinctHashedWords = item.Value.Distinct();

                    distinctHashedWords.AsParallel().ForAll(hashedWord =>
                    {
                        foreach (var userHashedWord in userHashedWords)
                            if (hashedWord.Equals(userHashedWord))
                                score += 1;
                    });

                    var match = (score == userHashedWords.Count);

                    if (match)
                        score += 1;

                    if (score > 0)
                    {
                        if (!result.ContainsKey(item.Key))
                            result.TryAdd(item.Key, score);
                        else
                            result[item.Key] += score;
                    }

                    if (match && matchStop)
                        throw new Exception("Match");
                });
            }
            catch (Exception) { }

            return result;
        }

        private static IDictionary<string, ulong> setDBGroupScore(ConcurrentBag<ulong> hashedWordList, bool matchStop, string connectionString)
        {
            var result = new ConcurrentDictionary<string, ulong>();
            SortedSet<ulong> userHashedWords = new SortedSet<ulong>(hashedWordList.ToList());

            KnowledgeRepository.Init(connectionString);
            var serverGroups = KnowledgeRepository.List();

            try
            {
                serverGroups.Where(itm => itm.Hashes.Any(itmv => userHashedWords.Contains((ulong)itmv.Value))).AsParallel().ForAll(group =>
                {
                    uint score = 0;
                    var groupHashes = KnowledgeRepository.Get(group.Id);

                    if ((groupHashes != null) && (groupHashes.Hashes != null))
                    {
                        var distinctHashedWords = groupHashes.Hashes.Distinct();

                        distinctHashedWords.AsParallel().ForAll(hashedWord =>
                        {
                            foreach (var userHashedWord in userHashedWords)
                                if (((ulong)hashedWord.Value).Equals(userHashedWord))
                                    score += 1;
                        });

                        var match = (score == userHashedWords.Count);

                        if (match)
                            score += 1;

                        if (score > 0)
                        {
                            if (!result.ContainsKey(group.Name))
                                result.TryAdd(group.Name, score);
                            else
                                result[group.Name] += score;
                        }

                        if (match && matchStop)
                            throw new Exception("Match");
                    }
                });
            }
            catch (Exception) { }

            return result;
        }

        private static Dictionary<string, ulong> setScorePercent(IOrderedEnumerable<KeyValuePair<string, ulong>> groupScore, int limit)
        {
            var result = new Dictionary<string, ulong>();

            if (groupScore.Any())
            {
                ulong maxPercent = 100;
                ulong maxScore = groupScore.Max(grp => grp.Value);

                foreach (var group in groupScore)
                {
                    var percent = ((group.Value * maxPercent) / maxScore);

                    if ((percent > 0) && ((limit == 0) || (result.Count < limit)))
                        result.Add(group.Key, percent);
                }
            }

            return result;
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