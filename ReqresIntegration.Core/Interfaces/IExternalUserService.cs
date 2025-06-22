using Practical.Core.Models;

namespace Practical.Core.Interfaces
{
    public interface IExternalUserService
    {
        Task<User> GetUserByIdAsync(int userId);
        Task<IEnumerable<User>> GetAllUsersAsync(int page);
    }
}
