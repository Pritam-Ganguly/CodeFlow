using CodeFlow.core.Models;

namespace CodeFlow.core.Repositories
{
    public interface IUserRepository
    {
        Task<User> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<UserProfile?> GetUserProfile(int id);
        Task<int?> CreateUserProfile(int id);
        Task<bool> UpdateUserProfileBio(int id, string bio);
        Task<bool> UpdateUserProfileImage(int id, string profilePictureMimeType, string profilePictureName, byte[] profilePicture);
    }
}