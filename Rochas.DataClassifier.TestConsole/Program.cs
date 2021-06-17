using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Rochas.DataClassifier.TestConsole.Mock;

namespace Rochas.DataClassifier.TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var sourceData = JsonConvert.DeserializeObject<ICollection<Nature>>(
                File.OpenText("Mock\\NatureData.json").ReadToEnd());

            using (var classifier = new RochasClassifier())
            {
                foreach (var item in sourceData)
                    classifier.AddGroup(item.Category);

                foreach (var item in sourceData)
                    classifier.Train(item.Category, string.Concat(item.Name, " ", item.Description));

                classifier.PrepareSearchTree();

                Console.WriteLine("Rochas Data Classifier");
                Console.WriteLine("----------------------");
                Console.WriteLine("Enter search term :");
                var searchTerm = Console.ReadLine();

                var result = classifier.Classify(searchTerm);
                var consoleResult = JsonConvert.SerializeObject(result, Formatting.Indented);

                Console.WriteLine();
                Console.WriteLine("Result :");
                Console.WriteLine(consoleResult);
                Console.WriteLine();
                Console.WriteLine("Press any key to exit.");
                Console.Read();
            }
        }
    }
}
