using System;
using System.Collections.Generic;
using Rochas.DataClassifier.Interfaces;

namespace Rochas.DataClassifier.ValueObjects
{
    public class KnowledgeGroup : IKnowledgeGroup
    {
        #region Constructors

        public KnowledgeGroup()
        {

        }

        public KnowledgeGroup(string name = "")
        {
            Name = name;
        }

        #endregion

        #region Public Properties

        public string Name { get; set; }

        public IEnumerable<IKnowledgeHash> Hashes { get; set; }

        #endregion
    }
}