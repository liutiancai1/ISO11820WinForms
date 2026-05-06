using System.ComponentModel.DataAnnotations.Schema;

namespace TestServer.Models
{
    public partial class Testmaster
    {
        [NotMapped]
        public bool UseFixedDuration { get; set; }

        [NotMapped]
        public int TargetDurationSeconds { get; set; } = 3600;
    }
}
