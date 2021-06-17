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

                var result = classifier.Classify("magnolia chinese");
            }
        }
    }
}
