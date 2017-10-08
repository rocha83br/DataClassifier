using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rochas.KnowledgeBaseServer.Models
{
    public class KnowledgeGroup
    {
        [Key]
        [Required]
        public string Name { get; set; }

        [Required]
        public virtual ICollection<KnowledgeHash> Hashes { get; set; }
    }
}