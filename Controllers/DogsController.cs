using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    [Authorize]
    public class DogsController(ApplicationDbContext context) : Controller
    {
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var dogs = await context.Set<Dog>().AsNoTracking().ToListAsync();
            return View(dogs);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var dog = await context.Set<Dog>().FindAsync(id);
            if (dog == null) return NotFound();
            return View(dog);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Dog dog)
        {
            if (!ModelState.IsValid) return View(dog);
            context.Set<Dog>().Add(dog);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var dog = await context.Set<Dog>().FindAsync(id);
            if (dog == null) return NotFound();
            return View(dog);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Dog dog)
        {
            if (id != dog.Id) return NotFound();
            if (!ModelState.IsValid) return View(dog);

            dog.UpdatedAt = DateTime.UtcNow;
            context.Set<Dog>().Update(dog);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var dog = await context.Set<Dog>().FindAsync(id);
            if (dog == null) return NotFound();
            return View(dog);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dog = await context.Set<Dog>().FindAsync(id);
            if (dog != null)
            {
                context.Set<Dog>().Remove(dog);
                await context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Acción para que un adoptante marque la mascota como adoptada
        [Authorize(Roles = "Adoptante")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adopt(int id)
        {
            var dog = await context.Set<Dog>().FindAsync(id);
            if (dog == null) return NotFound();
            if (dog.IsAdopted) return BadRequest("La mascota ya fue adoptada");

            dog.IsAdopted = true;
            dog.UpdatedAt = DateTime.UtcNow;
            context.Set<Dog>().Update(dog);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}
