using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Rochas.KnowledgeBaseServer.Models
{
    public class KnowledgeHash
    {
        [Key]
        [Required]
        [JsonIgnore]
        public int GroupName { get; set; }

        [Required]
        public int Value { get; set; }
    }
}