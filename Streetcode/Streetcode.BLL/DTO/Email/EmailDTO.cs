namespace Streetcode.BLL.DTO.Email
{
    public class EmailDTO
    {
        public Guid MessageId { get; set; }

        public string From { get; set; }

        public string Content { get; set; }
    }
}
