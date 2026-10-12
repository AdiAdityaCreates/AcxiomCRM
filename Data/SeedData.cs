using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var role in new[] { "Admin", "Manager", "SalesExecutive" })
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));

        // Development convenience only. Change this password immediately after first login.
        var email = "admin@acxiomcrm.local";
        var admin = await users.FindByEmailAsync(email);
        if (admin == null)
        {
            admin = new ApplicationUser { UserName = email, Email = email, FullName = "CRM Administrator", EmailConfirmed = true };
            var result = await users.CreateAsync(admin, "ChangeMe123!");
            if (result.Succeeded) await users.AddToRoleAsync(admin, "Admin");
        }
    }
}
