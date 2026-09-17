using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EcommerceApp.Models;
using EcommerceApp.Services;

namespace EcommerceApp.Controllers
{
    public class AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        SupabaseStorageService storage) : Controller
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Antes se guardaban en wwwroot/uploads/carnets; ahora suben
            // directo a Supabase Storage (bucket "carnets") y guardamos
            // la URL pública que nos devuelve.
            var urlAnverso = await storage.SubirArchivoAsync(model.FotoCarnetAnverso, "carnets", "anverso");
            var urlReverso = await storage.SubirArchivoAsync(model.FotoCarnetReverso, "carnets", "reverso");

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Edad = model.Edad,
                PhoneNumber = model.Telefono,
                NumeroCarnet = model.NumeroCarnet,
                FotoCarnetAnversoUrl = urlAnverso,
                FotoCarnetReversoUrl = urlReverso
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

            // Si ya inició sesión antes con este mismo proveedor, lo dejamos entrar directo
            var signInResult = await signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                var existingUser = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                return await RedirectSegunPerfil(existingUser);
            }

            // Primera vez con este proveedor: buscamos por email o creamos el usuario
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

        // Decide a dónde va cada usuario justo después de iniciar sesión,
        // según su rol. Se llama tanto desde Login como desde ExternalLoginCallback.
        private async Task<IActionResult> RedirectSegunPerfil(ApplicationUser? user)
        {
            if (user == null) return RedirectToAction(nameof(Login));

            var roles = await userManager.GetRolesAsync(user);

            // Zoonosis: va directo al panel de administración de mascotas.
            // No pasa por la verificación de carnet (esa validación es solo
            // para Adoptantes, no para el personal de Zoonosis).
            if (roles.Contains("Zoonosis"))
                return RedirectToAction("Administrar", "Mascotas");

            if (roles.Contains("Veterinario"))
                return RedirectToAction("Index", "Home"); // TODO: panel Veterinario

            // Resto de casos = Adoptante. Si le falta el carnet o las fotos
            // (por ejemplo, entró con Google/GitHub por primera vez),
            // lo mandamos a completar su perfil antes de usar la app.
            if (string.IsNullOrEmpty(user.NumeroCarnet) ||
                string.IsNullOrEmpty(user.FotoCarnetAnversoUrl) ||
                string.IsNullOrEmpty(user.FotoCarnetReversoUrl))
            {
                return RedirectToAction(nameof(CompletarPerfil));
            }

            return RedirectToAction("Index", "Mascotas");
        }

        // ---------- COMPLETAR PERFIL (para usuarios de login externo) ----------

        [HttpGet]
        public IActionResult CompletarPerfil() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompletarPerfil(CompletarPerfilViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var urlAnverso = await storage.SubirArchivoAsync(model.FotoCarnetAnverso, "carnets", "anverso");
            var urlReverso = await storage.SubirArchivoAsync(model.FotoCarnetReverso, "carnets", "reverso");

            user.Apellido = model.Apellido;
            user.Edad = model.Edad;
            user.PhoneNumber = model.Telefono;
            user.NumeroCarnet = model.NumeroCarnet;
            user.FotoCarnetAnversoUrl = urlAnverso;
            user.FotoCarnetReversoUrl = urlReverso;

            await userManager.UpdateAsync(user);

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