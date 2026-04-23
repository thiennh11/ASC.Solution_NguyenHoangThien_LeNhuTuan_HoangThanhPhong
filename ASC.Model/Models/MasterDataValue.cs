using ASC.Model.BaseTypes;
using ASC.Model.Models;

namespace ASC.Model
{
    public class MasterDataValue : BaseEntity
    {
        public int MasterDataKeyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public MasterDataKey? MasterDataKey { get; set; }
    }
}