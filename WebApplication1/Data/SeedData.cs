using Microsoft.AspNetCore.Identity;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles = { "Student", "Staff" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            string staffEmail = "suborna@iubat.edu";
            const string staffPassword = "Muna1234D";
            var existingStaff = await userManager.FindByEmailAsync(staffEmail);
            if (existingStaff == null)
            {
                var staffUser = new ApplicationUser
                {
                    UserName = staffEmail,
                    Email = staffEmail,
                    FirstName = "Suborna",
                    LastName = "IUBAT",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(staffUser, staffPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(staffUser, "Staff");
                }
            }
            else
            {
                // Ensure role and password are current (idempotent update for existing DB)
                if (!await userManager.IsInRoleAsync(existingStaff, "Staff"))
                    await userManager.AddToRoleAsync(existingStaff, "Staff");
                if (!await userManager.CheckPasswordAsync(existingStaff, staffPassword))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(existingStaff);
                    await userManager.ResetPasswordAsync(existingStaff, token, staffPassword);
                }
                // Keep profile names current
                if (existingStaff.FirstName != "Suborna" || existingStaff.LastName != "IUBAT")
                {
                    existingStaff.FirstName = "Suborna";
                    existingStaff.LastName = "IUBAT";
                    await userManager.UpdateAsync(existingStaff);
                }
            }
        }
    }
}
