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
        // returnUrl = "a dónde volver después de iniciar sesión".
        // Lo llena el sitio cuando alguien SIN sesión toca "Adoptar" en el
        // catálogo (ver Views/Mascotas/Index.cshtml): así, al terminar de
        // iniciar sesión, la persona cae directo en el formulario de adopción
        // de esa mascota en vez de perderse. Si entra al login directo, viene vacío.
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Se guarda en ViewData para que Login.cshtml lo reenvíe
            // en un campo oculto cuando la persona envíe el formulario.
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            // Se vuelve a guardar por si hay que mostrar el formulario otra vez
            // (datos inválidos / credenciales incorrectas) sin perder el destino.
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid) return View(model);

            var result = await signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await userManager.FindByEmailAsync(model.Email);
                return await RedirectSegunPerfil(user, returnUrl);
            }

            ModelState.AddModelError(string.Empty, "Credenciales inválidas");
            return View(model);
        }

        // returnUrl: igual que en Login, permite volver al formulario de adopción
        // después de crear la cuenta (si la persona venía de tocar "Adoptar").
        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // Registro simplificado: solo nombre, apellido, correo y contraseña.
        // Los datos "formales" (carnet, edad, domicilio, motivo, etc.) se
        // piden más adelante, al momento de solicitar una adopción.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            // Se guarda otra vez por si hay que mostrar el formulario con errores.
            ViewData["ReturnUrl"] = returnUrl;

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

                // Al terminar el registro se INICIA SESIÓN automáticamente (antes
                // mandaba al login y la persona tenía que escribir todo de nuevo).
                // Se hace DESPUÉS de asignar el rol, para que la sesión ya
                // nazca con el rol "Adoptante". Si venía de tocar "Adoptar",
                // RedirectSegunPerfil la devuelve al formulario de adopción.
                await signInManager.SignInAsync(user, isPersistent: false);
                return await RedirectSegunPerfil(user, returnUrl);
            }

            // Si falló (correo repetido, contraseña débil, etc.) se muestran los
            // errores EN ESPAÑOL (ver TraducirError, más abajo).
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, TraducirError(error));

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
                return await RedirectSegunPerfil(existingUser, returnUrl);
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

            return await RedirectSegunPerfil(user, returnUrl);
        }

        // Ya NO exige carnet/fotos completos para entrar a la app — eso
        // se pide únicamente cuando la persona solicita adoptar.
        // Decide a dónde mandar a la persona justo después de iniciar sesión.
        // returnUrl (opcional) = la página a la que quería llegar antes de que
        // le pidieran login (por ejemplo, el formulario de adopción).
        private async Task<IActionResult> RedirectSegunPerfil(ApplicationUser? user, string? returnUrl = null)
        {
            if (user == null) return RedirectToAction(nameof(Login));

            var roles = await userManager.GetRolesAsync(user);

            // Zoonosis y Veterinario siempre van a su panel: para ellos
            // "Adoptar" no aplica, así que ignoramos returnUrl.
            if (roles.Contains("Zoonosis"))
                return RedirectToAction("Administrar", "Mascotas");

            if (roles.Contains("Veterinario"))
                return RedirectToAction("Index", "Home"); // TODO: panel Veterinario

            // Adoptante: si venía de tocar "Adoptar", lo devolvemos ahí.
            // Url.IsLocalUrl es una protección de seguridad: solo aceptamos
            // rutas de ESTE sitio, nunca un link externo que alguien haya
            // metido a mano en la URL (ataque "open redirect").
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Mascotas");
        }

        // Pantalla que muestra el sitio cuando alguien intenta entrar a algo para
        // lo que su cuenta no tiene permiso (por ejemplo, tocar "Adoptar" con una
        // cuenta que no es Adoptante). Program.cs ya mandaba a "/Account/AccessDenied",
        // pero esta acción no existía y la persona veía un error 404.
        [HttpGet]
        public IActionResult AccessDenied() => View();

        // ASP.NET Identity devuelve sus errores en inglés. Aquí se traducen los
        // más comunes; si aparece uno que no está en la lista, se muestra tal cual.
        private static string TraducirError(IdentityError error) => error.Code switch
        {
            "DuplicateUserName" or "DuplicateEmail" => "Ya existe una cuenta con ese correo.",
            "InvalidEmail" or "InvalidUserName" => "El correo no es válido.",
            "PasswordTooShort" => "La contraseña debe tener al menos 6 caracteres.",
            "PasswordRequiresUpper" => "La contraseña debe tener al menos una letra mayúscula.",
            "PasswordRequiresLower" => "La contraseña debe tener al menos una letra minúscula.",
            "PasswordRequiresDigit" => "La contraseña debe tener al menos un número.",
            "PasswordRequiresNonAlphanumeric" => "La contraseña debe tener al menos un símbolo (por ejemplo # ! @ $).",
            "PasswordRequiresUniqueChars" => "La contraseña necesita más caracteres distintos.",
            _ => error.Description
        };

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            // Al cerrar sesión se vuelve al catálogo (la página principal del sitio).
            return RedirectToAction("Index", "Mascotas");
        }
    }
}