using IT15_MonticalboFinalProj.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IT15_MonticalboFinalProj.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public ProfileController(UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Setup()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        
        // If already complete, go to dashboard
        if (user.IsProfileComplete) return RedirectToAction("Index", "Home");

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Setup(ApplicationUser model, IFormFile? photo)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        user.FullName = model.FullName;
        user.Bio = model.Bio;
        user.Specialization = model.Specialization;
        user.StudentIdNumber = model.StudentIdNumber;
        user.ProgramOrCourse = model.ProgramOrCourse;

        if (photo != null)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
            var path = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            using (var stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }
            user.ProfilePhotoPath = "/uploads/profiles/" + fileName;
        }

        user.IsProfileComplete = true;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            TempData["SuccessMsg"] = "Profile completed successfully!";
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        return View(user);
    }
}
