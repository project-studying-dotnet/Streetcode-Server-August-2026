namespace Streetcode.Identity.WebApi.DTOs
{
    public class RegisterRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public string? Phone { get; set; }
        public string? Gender { get; set; }
    }
}