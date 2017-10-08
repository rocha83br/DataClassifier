using System;
using System.Collections;
using System.Collections.Generic;

namespace Rochas.DataClassifier.Interfaces
{
    public interface IKnowledgeGroup
    {
        string Name { get; set; }

        IEnumerable<IKnowledgeHash> Hashes { get; set; }
    }
}
