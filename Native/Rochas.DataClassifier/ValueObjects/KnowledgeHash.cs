using System;
using Rochas.DataClassifier.Interfaces;

namespace Rochas.DataClassifier.ValueObjects
{
    public class KnowledgeHash : IKnowledgeHash
    {
        #region Constructors

        public KnowledgeHash()
        {

        }

        public KnowledgeHash(int value)
        {
            Value = value;
        }

        #endregion

        #region Public Properties

        public int Value { get; set; }

        #endregion
    }
}