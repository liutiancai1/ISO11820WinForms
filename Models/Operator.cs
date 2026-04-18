using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TestServer.Models
{
    [Keyless]
    [Table("operators")]
    public partial class Operator
    {
        [Required]
        [Column("userid")]
        [StringLength(16)]
        [Unicode(false)]
        public string Userid { get; set; }
        [Required]
        [Column("username")]
        [StringLength(64)]
        [Unicode(false)]
        public string Username { get; set; }
        [Required]
        [Column("pwd")]
        [StringLength(128)]
        [Unicode(false)]
        public string Pwd { get; set; }
        [Required]
        [Column("usertype")]
        [StringLength(8)]
        [Unicode(false)]
        public string Usertype { get; set; }
    }
}
