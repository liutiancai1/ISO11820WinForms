using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TestServer.Models
{
    [Table("apparatus")]
    public partial class Apparatus
    {
        [Key]
        [Column("apparatusid")]
        public int Apparatusid { get; set; }
        [Required]
        [Column("innernumber")]
        [StringLength(50)]
        [Unicode(false)]
        public string Innernumber { get; set; }
        [Required]
        [Column("apparatusname")]
        [StringLength(512)]
        [Unicode(false)]
        public string Apparatusname { get; set; }
        [Column("checkdatef", TypeName = "date")]
        public DateTime Checkdatef { get; set; }
        [Column("checkdatet", TypeName = "date")]
        public DateTime Checkdatet { get; set; }
        [Required]
        [Column("pidport")]
        [StringLength(50)]
        [Unicode(false)]
        public string Pidport { get; set; }
        [Required]
        [Column("powerport")]
        [StringLength(50)]
        [Unicode(false)]
        public string Powerport { get; set; }
        [Column("constpower")]
        public int? Constpower { get; set; }
    }
}
