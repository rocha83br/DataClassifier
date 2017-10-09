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

        public KnowledgeHash(int value)
        {
            this.Value = value;
        }

        #endregion

        #region Public Properties

        [Key]
        [Required]
        [JsonIgnore]
        public int GroupId { get; set; }

        [Required]
        public int Value { get; set; }

        #endregion
    }
}