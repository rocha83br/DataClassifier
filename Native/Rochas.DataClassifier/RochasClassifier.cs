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
        ushort dataCleanAdjustRatio;

        readonly static ConcurrentBag<string> groupList = new ConcurrentBag<string>();
        static Dictionary<string, SortedDictionary<ulong, uint>> searchTree = new Dictionary<string, SortedDictionary<ulong, uint>>();
        readonly static ConcurrentDictionary<string, ConcurrentDictionary<ulong, uint>> hashedTree = new ConcurrentDictionary<string, ConcurrentDictionary<ulong, uint>>();

        readonly static string languageChars = "àáãçéêíóõúÀÁÃÇÉÊÍÓÕÚ";
        readonly static string cleanLanguageChars = "aaaceeioouAAACEEIOOU";

        readonly static string[] specialChars = { "@", "%", "#", "/", "\\", ";", ":", ".", ",", "*", "(", ")", "[", "]", "<", ">", "+", "-", "\"", "'", "´", "`", "?", "!" };

        readonly static string[] skipWords = new[] {
        // Portuguese
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
        "tenham","tivesse","tivéssemos","tivessem","tiver","tivermos","tiverem","terei","terá","teremos","terão","teria","teríamos","teriam",
        // English
         "about", "above","after","again","against","all","am","an","and","any","are","aren't","as","at","be","because","been","before","being","below","between","both",
         "but","by","can't","cannot","could","couldn't","did","didn't","does","doesn't","doing","don't","down","during","each","few","for","from","further","had","hadn't",
         "has","hasn't","have","haven't","having","he","he'd","he'll","he's","her","here","here's","hers","herself","him","himself","his","how","how's","i","i'd","i'll",
         "i'm","i've","if","in","into","is","isn't","it","it's","its","itself","let's","me","more","most","mustn't","my","myself","nor","not","of","off","on","once","only",
         "or","other","ought","our","ours","out","over","own","same","shan't","she","she'd","she'll","she's","should","shouldn't","so","some","such","than","that","that's",
         "the","their","theirs","them","themselves","then","there","there's","these","they","they'd","they'll","they're","they've","this","those","through","to","too","under",
         "until","up","very","was","wasn't","we","we'd","we'll","we're","we've","were","weren't","what","what's","when","when's","where","where's","which","while","who",
         "who's","whom","why","why's","with","won't","would","wouldn't","you","you'd","you'll","you're","you've","your","yours","yourself","yourselves" };

        #endregion

        #region Constructors

        public RochasClassifier(bool allowRepeat = false, bool filterChars = false, bool sensitiveCase = false, PhoneticMatchType phoneticMatchType = PhoneticMatchType.None, ushort cleanAdjustRatio = 100, string groupSeparator = "")
        {
            allowHashRepetition = allowRepeat;
            useSpecialCharsFilter = filterChars;
            useSensitiveCase = sensitiveCase;
            phoneticType = phoneticMatchType;
            dataCleanAdjustRatio = cleanAdjustRatio;            
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
            ConcurrentDictionary<ulong, uint> hashedWordList = null;

            if (!hashedTree.ContainsKey(group))
                hashedWordList = new ConcurrentDictionary<ulong, uint>();
            else
                hashedWordList = hashedTree[group];

            if (useSpecialCharsFilter)
                text = filterSpecialChars(text);

            foreach (var word in text.Trim().Split(' '))
                stemmHash(word, hashedWordList, group);

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

            cleanIrrelevantTrainingData();

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

            searchTree = JsonConvert.DeserializeObject<Dictionary<string, SortedDictionary<ulong, uint>>>(content);

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                persistKnowledgeDB(connectionString);

                searchTree.Clear();
                searchTree = null;
            }
        }

        public IDictionary<string, uint> Classify(string text, int limit = 0, bool matchStop = true, string connectionString = "")
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentNullException("text");

            var hashedWordList = new ConcurrentDictionary<ulong, uint>();

            if (useSpecialCharsFilter)
                text = filterSpecialChars(text);

            text.Trim().ToLower().Split(' ').AsParallel().ForAll(word =>
            {
                stemmHash(word, hashedWordList);
            });

            IDictionary<string, uint> result = null;
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

        private void stemmHash(string word, ConcurrentDictionary<ulong, uint> hashedWordList, string group = "")
        {
            var trimmedWord = word.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedWord) && !group.Equals(word) && !groupContentSeparator.Equals(trimmedWord))
            {
                using (var stemmer = PTStemmer.Stemmer.StemmerFactory())
                {
                    string treatedWord = string.Empty;
                    ulong hashedWord = 0;

                    treatedWord = trimmedWord.ToLower();

                    if (!skipWords.Contains(treatedWord))
                    {
                        stemmer.DisableCaching();
                        treatedWord = stemmer.Stemming(treatedWord);

                        if (phoneticType != PhoneticMatchType.None)
                            treatedWord = filterLanguageChars(treatedWord);

                        hashedWord = treatedWord.GetCustomHashCode();

                        addHashedWord(hashedWord, hashedWordList);

                        if (phoneticType == PhoneticMatchType.UseSondexAlgorithm)
                        {
                            var soundExWord = RochasSoundEx.Generate(treatedWord);
                            addHashedWord(soundExWord.GetCustomHashCode(), hashedWordList);
                        }
                    }
                }
            }
        }

        private void addHashedWord(ulong hashedWord, ConcurrentDictionary<ulong, uint> hashedWordList)
        {
            if (allowHashRepetition)
                hashedWordList.TryAdd(hashedWord, 0);
            else
            {
                if (!hashedWordList.ContainsKey(hashedWord))
                    hashedWordList.TryAdd(hashedWord, 1);
                else
                    hashedWordList[hashedWord] += 1;
            }
        }

        private static void prepareSearchTree()
        {
            foreach (var item in hashedTree)
            {
                searchTree.Add(item.Key, new SortedDictionary<ulong, uint>());

                foreach (var itemValue in item.Value)
                    searchTree[item.Key].Add(itemValue.Key, itemValue.Value);
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
            Console.WriteLine(string.Format("Training process finished in {0} minutes.", lastElapsedMinutes));
            Console.WriteLine();
        }

        private void cleanIrrelevantTrainingData()
        {
            var startTime = DateTime.Now;
            Console.WriteLine("- Start cleaning irrelevant data...");
            Console.WriteLine();

            hashedTree.AsParallel().ForAll(group =>
            {
                var groupWordsRelevance = group.Value.Sum(gpv => gpv.Value);
                var groupWordsCount = group.Value.Count();
                var cutRatio = groupWordsRelevance / (groupWordsCount * (dataCleanAdjustRatio / 100.0));

                uint fake;
                foreach (var word in group.Value)
                    if (word.Value <= cutRatio)
                        group.Value.TryRemove(word.Key, out fake);
            });

            var lastElapsedMinutes = Math.Round((DateTime.Now - startTime).TotalMinutes, 0);

            Console.WriteLine(string.Format("- Cleaning process finished in {0} minutes.", lastElapsedMinutes));
            Console.WriteLine();
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

        private IDictionary<string, uint> setGroupScore(ConcurrentDictionary<ulong, uint> hashedWordList, bool matchStop)
        {
            var result = new ConcurrentDictionary<string, uint>();
            SortedSet<ulong> userHashedWords = new SortedSet<ulong>(hashedWordList.Keys);

            try
            {
                searchTree.Where(itm => itm.Value.Any(itmv => userHashedWords.Contains(itmv.Key))).AsParallel().ForAll(item =>
                {
                    uint score = 0;
                    var hashedWords =  allowHashRepetition ? item.Value : item.Value.Distinct();

                    hashedWords.AsParallel().ForAll(hashedWord =>
                    {
                        foreach (var userHashedWord in userHashedWords)
                            if (hashedWord.Key.Equals(userHashedWord))
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

        private IDictionary<string, uint> setDBGroupScore(ConcurrentDictionary<ulong, uint> hashedWordList, bool matchStop, string connectionString)
        {
            var result = new ConcurrentDictionary<string, uint>();
            SortedSet<ulong> userHashedWords = new SortedSet<ulong>(hashedWordList.Keys);

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
                        var hashedWords = allowHashRepetition ? groupHashes.Hashes : groupHashes.Hashes.Distinct();

                        hashedWords.AsParallel().ForAll(hashedWord =>
                        {
                            foreach (var userHashedWord in userHashedWords)
                                if (((uint)hashedWord.Value).Equals(userHashedWord))
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

        private static Dictionary<string, uint> setScorePercent(IOrderedEnumerable<KeyValuePair<string, uint>> groupScore, int limit)
        {
            var result = new Dictionary<string, uint>();

            if (groupScore.Any())
            {
                uint maxPercent = 100;
                uint maxScore = groupScore.Max(grp => grp.Value);

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