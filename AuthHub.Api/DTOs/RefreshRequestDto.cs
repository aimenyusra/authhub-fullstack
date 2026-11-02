using System.ComponentModel.DataAnnotations;

namespace AuthHub.Api.DTOs
{
    public class RefreshRequestDto
    {
        [Required]
     public string RefreshToken { get; set; }= string.Empty;
    }
}
