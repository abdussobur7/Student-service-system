using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Student");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                // Normalize email - trim and prevent case/space issues that cause "Invalid login attempt"
                var email = model.Email?.Trim();
                if (string.IsNullOrWhiteSpace(email))
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }

                // Look up by Email (not UserName) for reliability - handles existing DB where UserName may differ
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    Console.WriteLine($"[Login] Failed: no user found for email '{email}'");
                    ModelState.AddModelError(string.Empty, "Invalid login attempt. No account found for this email.");
                    return View(model);
                }

                // Use UserName from DB for sign-in (PasswordSignInAsync looks up by UserName)
                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    if (await _userManager.IsInRoleAsync(user, "Staff"))
                    {
                        return RedirectToAction("AllRequests", "Staff");
                    }
                    return RedirectToAction("MyRequests", "Student");
                }

                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "Account locked out. Try again later.");
                    return View(model);
                }
                if (result.IsNotAllowed)
                {
                    ModelState.AddModelError(string.Empty, "Login not allowed. Ensure email is confirmed.");
                    return View(model);
                }

                // Check password explicitly for better diagnostics (logged server-side, generic message to user)
                var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
                Console.WriteLine($"[Login] Failed for '{email}': PasswordValid={passwordValid}, IsLockedOut={result.IsLockedOut}, IsNotAllowed={result.IsNotAllowed}");
                if (!passwordValid)
                    ModelState.AddModelError(string.Empty, "Invalid login attempt. Incorrect password.");
                else
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
