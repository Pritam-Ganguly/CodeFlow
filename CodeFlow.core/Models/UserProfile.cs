namespace CodeFlow.core.Models
{
    public class UserProfile
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Bio { get; set; } = string.Empty;
        public byte[] ProfilePicture { get; set; } = [];
        public string ProfilePictureMimeType { get; set; } = string.Empty;
        public string ProfilePictureFileName { get; set; } = string.Empty;
    }
}
