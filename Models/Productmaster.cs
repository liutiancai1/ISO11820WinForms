using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TestServer.Models
{
    [Table("productmaster")]
    public partial class Productmaster
    {
        public Productmaster()
        {
            Testmasters = new HashSet<Testmaster>();
        }

        [Key]
        [Column("productid")]
        [StringLength(50)]
        [Unicode(false)]
        public string Productid { get; set; } = string.Empty;
        
        [Required]
        [Column("productname")]
        [StringLength(1024)]
        [Unicode(false)]
        public string Productname { get; set; } = string.Empty;
        
        [Required]
        [Column("specific")]
        [StringLength(1024)]
        [Unicode(false)]
        public string Specific { get; set; } = string.Empty;
        
        [Column("diameter")]
        public double Diameter { get; set; }
        
        [Column("height")]
        public double Height { get; set; }
        
        // flag 在数据库中允许 NULL
        [Column("flag")]
        public string? Flag { get; set; }
        
        [InverseProperty("Product")]
        public virtual ICollection<Testmaster> Testmasters { get; set; }
    }
}
