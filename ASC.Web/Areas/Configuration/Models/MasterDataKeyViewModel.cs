using System.ComponentModel.DataAnnotations;

namespace ASC.Web.Areas.Configuration.Models
{
    public class MasterDataKeyViewModel
    {
        public string? RowKey { get; set; }
        public string? PartitionKey { get; set; }
        public bool IsActive { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}