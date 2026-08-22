using System.ComponentModel.DataAnnotations;

namespace Streetcode.BLL.DTO.Streetcode.TextContent.Text
{
  public class TextCreateDTO
  {
        [Required]
        [MaxLength(50)]
        public string Title { get; set; }
        [MaxLength(25000)]
        public string TextContent { get; set; }
        [MaxLength(200)]
        public string? AdditionalText { get; set; }
  }
}
