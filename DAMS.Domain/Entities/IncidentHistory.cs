using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAMS.Domain.Entities
{
    public class IncidentHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int AssetId { get; set; }
        public int IncidentId { get; set; }
        public int KpiId { get; set; }
        public string IncidentTitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int SeverityId { get; set; }
        public int StatusId { get; set; }
        public string AssignedTo { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        // Navigation properties
        public CommonLookup Severity { get; set; } = null!;
        public CommonLookup Status { get; set; } = null!;
        public Incident Incident { get; set; } = null!;
        public Asset Asset { get; set; } = null!;
        public KpisLov KpisLov { get; set; } = null!;
    }
}
