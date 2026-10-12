using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole> _roles;
    public UsersController(UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles) { _users = users; _roles = roles; }
    public async Task<IActionResult> Index() => View(await _users.Users.OrderBy(x => x.Email).ToListAsync());
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null) return NotFound();
        ViewBag.Roles = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await _roles.Roles.ToListAsync(), "Name", "Name", (await _users.GetRolesAsync(user)).FirstOrDefault());
        return View(user);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, string role, bool isActive)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null) return NotFound();
        if (!await _roles.RoleExistsAsync(role)) ModelState.AddModelError("", "Invalid role.");
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await _roles.Roles.ToListAsync(), "Name", "Name", role);
            return View(user);
        }
        var current = await _users.GetRolesAsync(user);
        if (current.Count > 0) await _users.RemoveFromRolesAsync(user, current);
        await _users.AddToRoleAsync(user, role);
        user.IsActive = isActive;
        await _users.UpdateAsync(user);
        return RedirectToAction(nameof(Index));
    }
}
