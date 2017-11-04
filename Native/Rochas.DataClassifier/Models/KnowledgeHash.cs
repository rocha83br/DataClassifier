using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Rochas.DataClassifier.Models
{
    public class KnowledgeHash
    {
        #region Constructors

        public KnowledgeHash()
        {

        }

        public KnowledgeHash(int hash, int relevance)
        {
            this.Hash = hash;
            this.Relevance = relevance;
        }

        #endregion

        #region Public Properties

        [Key]
        [Required]
        [JsonIgnore]
        public int GroupId { get; set; }

        [Required]
        public int Hash { get; set; }

        [Required]
        public int Relevance { get; set; }

        #endregion
    }
}