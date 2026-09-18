using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    public class AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager) : Controller
    {
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await userManager.FindByEmailAsync(model.Email);
                return await RedirectSegunPerfil(user);
            }

            ModelState.AddModelError(string.Empty, "Credenciales inválidas");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register() => View();

        // Registro simplificado: solo nombre, apellido, correo y contraseña.
        // Los datos "formales" (carnet, edad, domicilio, motivo, etc.) se
        // piden más adelante, al momento de solicitar una adopción.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Nombre = model.Nombre,
                Apellido = model.Apellido
            };

            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                if (!await roleManager.RoleExistsAsync("Adoptante"))
                    await roleManager.CreateAsync(new IdentityRole("Adoptante"));

                await userManager.AddToRoleAsync(user, "Adoptante");

                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // ---------- LOGIN EXTERNO (Google / GitHub) ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error del proveedor externo: {remoteError}");
                return RedirectToAction(nameof(Login));
            }

            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null) return RedirectToAction(nameof(Login));

            var signInResult = await signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                var existingUser = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                return await RedirectSegunPerfil(existingUser);
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var nombre = info.Principal.FindFirstValue(ClaimTypes.GivenName)
                         ?? info.Principal.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "El proveedor externo no compartió un correo electrónico.");
                return RedirectToAction(nameof(Login));
            }

            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                // Igual que el registro normal: solo lo básico por ahora.
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    Nombre = nombre ?? string.Empty,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                    return RedirectToAction(nameof(Login));
                }

                if (!await roleManager.RoleExistsAsync("Adoptante"))
                    await roleManager.CreateAsync(new IdentityRole("Adoptante"));

                await userManager.AddToRoleAsync(user, "Adoptante");
            }

            await userManager.AddLoginAsync(user, info);
            await signInManager.SignInAsync(user, isPersistent: false);

            return await RedirectSegunPerfil(user);
        }

        // Ya NO exige carnet/fotos completos para entrar a la app — eso
        // se pide únicamente cuando la persona solicita adoptar.
        private async Task<IActionResult> RedirectSegunPerfil(ApplicationUser? user)
        {
            if (user == null) return RedirectToAction(nameof(Login));

            var roles = await userManager.GetRolesAsync(user);

            if (roles.Contains("Zoonosis"))
                return RedirectToAction("Administrar", "Mascotas");

            if (roles.Contains("Veterinario"))
                return RedirectToAction("Index", "Home"); // TODO: panel Veterinario

            return RedirectToAction("Index", "Mascotas");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}