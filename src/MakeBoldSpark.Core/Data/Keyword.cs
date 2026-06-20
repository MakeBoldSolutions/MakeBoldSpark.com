using System.Text.Json.Serialization;

namespace MakeBoldSpark.Core.Data
{
    public partial class Keyword : BaseEntity
    {
        public required string Name { get; set; } = string.Empty;
        public required string Description { get; set; } = string.Empty;
        [JsonIgnore] public virtual ICollection<Menu> Menus { get; set; } = [];
        [JsonIgnore] public virtual ICollection<ContentPart> ContentParts { get; set; } = [];
    }
}
