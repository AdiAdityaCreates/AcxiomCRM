using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly IAuditService _audit;
    public AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, IAuditService audit)
    { _users = users; _signIn = signIn; _audit = audit; }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) { ViewBag.ReturnUrl = returnUrl; return View(); }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _users.FindByEmailAsync(model.Email);
        if (user == null || !user.IsActive)
        {
            await _audit.LogAsync("Failed Login", "Authentication", model.Email, result: "Failure");
            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }
        var result = await _signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        await _audit.LogAsync(result.Succeeded ? "Login" : "Failed Login", "Authentication", user.Id, result: result.Succeeded ? "Success" : "Failure");
        if (result.Succeeded) return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction("Index", "Home");
        ModelState.AddModelError("", result.IsLockedOut ? "Account temporarily locked. Try again later." : "Invalid login attempt.");
        return View(model);
    }

    [AllowAnonymous]
    public IActionResult Register() => View();

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = new ApplicationUser { UserName = model.Email, Email = model.Email, FullName = model.FullName };
        var result = await _users.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            // Public self-registration receives the least-privileged role.
            await _users.AddToRoleAsync(user, "SalesExecutive");
            await _signIn.SignInAsync(user, isPersistent: false);
            await _audit.LogAsync("Register", "User", user.Id);
            return RedirectToAction("Index", "Home");
        }
        foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        return View(model);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var id = _users.GetUserId(User);
        await _signIn.SignOutAsync();
        await _audit.LogAsync("Logout", "Authentication", id);
        return RedirectToAction("Login");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
