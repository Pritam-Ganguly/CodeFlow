using CodeFlow.core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CodeFlow.core.Repositories.Seed
{
    public class UserDataSeed
    {
        public static async Task Initialize(IServiceProvider serviceProvider, string testUserPw)
        {
            var adminID = await EnsureUser(serviceProvider, testUserPw, "admin@codeflow.com");
            await EnsureRole(serviceProvider, adminID, "ADMIN");

        }

        private static async Task<int> EnsureUser(IServiceProvider serviceProvider,
                                                    string testUserPw, string UserName)
        {
            var userManager = serviceProvider.GetService<UserManager<ApplicationUser>>();

            var user = await userManager!.FindByNameAsync(UserName);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = UserName,
                    Email = UserName,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user, testUserPw);
            }

            if (user == null)
            {
                throw new Exception("The password is probably not strong enough!");
            }

            return user.Id;
        }

        private static async Task<IdentityResult> EnsureRole(IServiceProvider serviceProvider,
                                                                      int id, string role)
        {
            var roleManager = serviceProvider.GetService<RoleManager<ApplicationRole>>();

            if (roleManager == null)
            {
                throw new Exception("roleManager null");
            }

            IdentityResult IR;
            if (!await roleManager.RoleExistsAsync(role))
            {
                IR = await roleManager.CreateAsync(new ApplicationRole()
                {
                    Name = role,
                });
            }

            var userManager = serviceProvider.GetService<UserManager<ApplicationUser>>();

            var user = await userManager!.FindByIdAsync(id.ToString());

            if (user == null)
            {
                throw new Exception("The testUserPw password was probably not strong enough!");
            }

            IR = await userManager.AddToRoleAsync(user, role);

            return IR;
        }

        public static void SeedDB(string adminID)
        { 
            
        }
    }
}
