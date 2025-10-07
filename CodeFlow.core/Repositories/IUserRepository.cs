using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeFlow.core.Models;

namespace CodeFlow.core.Repositories
{
    public interface IUserRepository
    {
        Task<User> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<int> CreateAsync(User user);
    }
}