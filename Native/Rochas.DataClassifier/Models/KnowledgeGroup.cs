using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Rochas.DataClassifier.Models
{
    public class KnowledgeGroup
    {
        #region Constructors

        public KnowledgeGroup()
        {

        }

        public KnowledgeGroup(string name)
        {
            this.Name = name;
        }

        #endregion

        #region Public Properties

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public virtual ICollection<KnowledgeHash> Hashes { get; set; }

        #endregion
    }
}