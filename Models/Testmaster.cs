using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TestServer.Models
{
    [Table("testmaster")]
    public partial class Testmaster
    {
        [Key]
        [Column("productid")]
        [StringLength(50)]
        [Unicode(false)]
        public string Productid { get; set; } = string.Empty;
        
        [Key]
        [Column("testid")]
        [StringLength(50)]
        [Unicode(false)]
        public string Testid { get; set; } = string.Empty;
        
        [Column("ambtemp")]
        public double Ambtemp { get; set; }
        
        [Column("ambhumi")]
        public double Ambhumi { get; set; }
        
        [Column("testdate", TypeName = "date")]
        public DateTime Testdate { get; set; }
        
        [Column("totaltesttime")]
        public int Totaltesttime { get; set; }
        
        [Required]
        [Column("according")]
        [StringLength(1024)]
        [Unicode(false)]
        public string According { get; set; } = string.Empty;
        
        [Required]
        [Column("operator")]
        [StringLength(50)]
        [Unicode(false)]
        public string Operator { get; set; } = string.Empty;
        
        [Required]
        [Column("apparatusid")]
        [StringLength(16)]
        [Unicode(false)]
        public string Apparatusid { get; set; } = string.Empty;

        [Required]
        [Column("apparatusname")]
        [StringLength(50)]
        [Unicode(false)]
        public string Apparatusname { get; set; } = string.Empty;
        
        [Column("apparatuschkdate", TypeName = "date")]
        public DateTime Apparatuschkdate { get; set; }
        
        [Column("constpower")]
        public int Constpower { get; set; }
        
        [Required]
        [Column("rptno")]
        [StringLength(50)]
        [Unicode(false)]
        public string Rptno { get; set; } = string.Empty;
        
        [Column("preweight")]
        public double Preweight { get; set; }
        
        [Column("postweight")]
        public double Postweight { get; set; }
        
        [Column("lostweight")]
        public double Lostweight { get; set; }
        
        /// <summary>
        /// [判定项]样品质量失重率
        /// </summary>
        [Column("lostweight_per")]
        public double LostweightPer { get; set; }
        
        [Required]
        [Column("phenocode")]
        [StringLength(12)]
        [Unicode(false)]
        public string Phenocode { get; set; } = string.Empty;
        
        [Column("flametime")]
        public int Flametime { get; set; }
        
        [Column("flameduration")]
        public int Flameduration { get; set; }
        
        [Column("maxtf1")]
        public double Maxtf1 { get; set; }
        
        [Column("maxtf2")]
        public double Maxtf2 { get; set; }
        
        [Column("maxts")]
        public double Maxts { get; set; }
        
        [Column("maxtc")]
        public double Maxtc { get; set; }
        
        [Column("maxtf1_time")]
        public int Maxtf1Time { get; set; }
        
        [Column("maxtf2_time")]
        public int Maxtf2Time { get; set; }
        
        [Column("maxts_time")]
        public int MaxtsTime { get; set; }
        
        [Column("maxtc_time")]
        public int MaxtcTime { get; set; }
        
        [Column("finaltf1_time")]
        public int Finaltf1Time { get; set; }
        
        [Column("finaltf2_time")]
        public int Finaltf2Time { get; set; }
        
        [Column("finalts_time")]
        public int FinaltsTime { get; set; }
        
        [Column("finaltc_time")]
        public int FinaltcTime { get; set; }
        
        [Column("finaltf1")]
        public double Finaltf1 { get; set; }
        
        [Column("finaltf2")]
        public double Finaltf2 { get; set; }
        
        [Column("finalts")]
        public double Finalts { get; set; }
        
        [Column("finaltc")]
        public double Finaltc { get; set; }
        
        [Column("deltatf1")]
        public double Deltatf1 { get; set; }
        
        [Column("deltatf2")]
        public double Deltatf2 { get; set; }
        
        /// <summary>
        /// [判定项]本次试验样品的最终温升
        /// </summary>
        [Column("deltatf")]
        public double Deltatf { get; set; }
        
        [Column("deltats")]
        public double Deltats { get; set; }
        
        [Column("deltatc")]
        public double Deltatc { get; set; }
        
        // memo 和 flag 在数据库中允许 NULL
        [Column("memo")]
        [Unicode(false)]
        public string? Memo { get; set; }
        
        [Column("flag")]
        public string? Flag { get; set; }
        
        [ForeignKey("Productid")]
        [InverseProperty("Testmasters")]
        public virtual Productmaster? Product { get; set; }
    }
}
