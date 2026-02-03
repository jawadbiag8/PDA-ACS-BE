using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAMS.Domain.Entities
{
    public class KPIsResultHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int AssetId { get; set; }
        public int KpiId { get; set; }
        public string Result { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int KPIsResultId { get; set; }

        // Navigation properties
        public Asset Asset { get; set; } = null!;
        public KpisLov KpisLov { get; set; } = null!;
        public KPIsResult KPIsResult { get; set; } = null!;
    }
}
