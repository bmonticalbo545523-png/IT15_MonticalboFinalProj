using IT15_MonticalboFinalProj.Models;
using IT15_MonticalboFinalProj.Models.ViewModels;
using IT15_MonticalboFinalProj.Repositories;
using IT15_MonticalboFinalProj.Services;
using QRCoder;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IT15_MonticalboFinalProj.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditLogRepository _auditLogs;
    private readonly RecaptchaService _recaptchaService;
    private readonly IDepartmentRepository _deptRepo;
    private readonly UrlEncoder _urlEncoder;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IAuditLogRepository auditLogs,
        RecaptchaService recaptchaService,
        IDepartmentRepository deptRepo,
        UrlEncoder urlEncoder)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _auditLogs = auditLogs;
        _recaptchaService = recaptchaService;
        _deptRepo = deptRepo;
        _urlEncoder = urlEncoder;
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return await RedirectToDashboard();
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var recaptchaResponse = Request.Form["g-recaptcha-response"].ToString() ?? string.Empty;
        var isRecaptchaValid = await _recaptchaService.ValidateRecaptchaAsync(recaptchaResponse);
        if (!isRecaptchaValid)
        {
            ModelState.AddModelError("", "Please complete the reCAPTCHA to proceed.");
            return View(model);
        }

        // Allow login by username or email
        var user = model.UsernameOrEmail.Contains('@')
            ? await _userManager.FindByEmailAsync(model.UsernameOrEmail)
            : await _userManager.FindByNameAsync(model.UsernameOrEmail);

        if (user == null)
        {
            ModelState.AddModelError("", "Invalid username or password.");
            return View(model);
        }

        // Check if user is SuperAdmin to bypass deactivation check
        var isSuperAdmin = await _userManager.IsInRoleAsync(user, "SuperAdmin");
        if (!isSuperAdmin)
        {
            if (user.Status == "Inactive" || !user.IsActive)
            {
                ModelState.AddModelError("", "Your account is inactive. Please contact support.");
                return View(model);
            }

            if (user.DepartmentId.HasValue)
            {
                var dept = await _deptRepo.GetByIdAsync(user.DepartmentId.Value);
                if (dept != null && !dept.IsActive)
                {
                    ModelState.AddModelError("", "Your department has been archived. Access is restricted.");
                    return View(model);
                }
            }
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (result.RequiresTwoFactor)
        {
            return RedirectToAction("Verify2FA", new { returnUrl, rememberMe = model.RememberMe });
        }

        if (result.Succeeded)
        {
            await _auditLogs.AddAsync(new AuditLog
            {
                UserId = user.Id,
                Action = $"User '{user.UserName}' logged in",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Timestamp = DateTime.UtcNow
            });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return await RedirectToDashboard();
        }

        ModelState.AddModelError("", "Invalid username or password.");
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return await RedirectToDashboard();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var recaptchaResponse = Request.Form["g-recaptcha-response"].ToString() ?? string.Empty;
        var isRecaptchaValid = await _recaptchaService.ValidateRecaptchaAsync(recaptchaResponse);
        if (!isRecaptchaValid)
        {
            ModelState.AddModelError("", "Please complete the reCAPTCHA to proceed.");
            return View(model);
        }

        // Duplicate email check
        if (await _userManager.FindByEmailAsync(model.Email) != null)
        {
            ModelState.AddModelError("Email", "An account with this email already exists.");
            return View(model);
        }

        // Duplicate username check
        if (await _userManager.FindByNameAsync(model.Username) != null)
        {
            ModelState.AddModelError("Username", "This username is already taken.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            FullName = model.FullName,
            Email = model.Email,
            UserName = model.Username,
            EmailConfirmed = true,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            IsProfileComplete = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Student");

            await _auditLogs.AddAsync(new AuditLog
            {
                UserId = user.Id,
                Action = $"New student '{user.UserName}' registered.",
                Timestamp = DateTime.UtcNow
            });

            TempData["SuccessMsg"] = "Account created successfully. Please log in.";
            return RedirectToAction("Login");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            await _auditLogs.AddAsync(new AuditLog
            {
                UserId = user.Id,
                Action = $"User '{user.UserName}' logged out",
                Timestamp = DateTime.UtcNow
            });
        }
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    [HttpGet]
    public async Task<IActionResult> Enable2FA()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login");

        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        var email = await _userManager.GetEmailAsync(user);
        var authenticatorUri = string.Format(
            "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6",
            _urlEncoder.Encode("Projexis"),
            _urlEncoder.Encode(email!),
            unformattedKey);

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20);
        
        var model = new TwoFactorSetupViewModel
        {
            SharedKey = unformattedKey!,
            QrCodeBase64 = Convert.ToBase64String(qrCodeImage)
        };

        return View(model);
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> Enable2FA(TwoFactorSetupViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login");

        if (!ModelState.IsValid) return View(model);

        var verificationCode = model.VerificationCode.Replace(" ", string.Empty).Replace("-", string.Empty);
        var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

        if (!is2faTokenValid)
        {
            ModelState.AddModelError("VerificationCode", "Verification code is invalid.");
            return await Enable2FA(); 
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        await _auditLogs.AddAsync(new AuditLog { UserId = user.Id, Action = "User enabled 2FA", Timestamp = DateTime.UtcNow });

        TempData["SuccessMsg"] = "Two-factor authentication enabled successfully.";
        return await RedirectToDashboard();
    }

    [HttpGet]
    public async Task<IActionResult> Verify2FA(string? returnUrl = null, bool rememberMe = false)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null) return RedirectToAction("Login");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new TwoFactorViewModel { RememberMe = rememberMe, ReturnUrl = returnUrl });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify2FA(TwoFactorViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null) return RedirectToAction("Login");

        var verificationCode = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(verificationCode, model.RememberMe, rememberClient: false);

        if (result.Succeeded)
        {
            await _auditLogs.AddAsync(new AuditLog { UserId = user.Id, Action = "User logged in with 2FA", Timestamp = DateTime.UtcNow });
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)) return Redirect(model.ReturnUrl);
            return await RedirectToDashboard();
        }

        ModelState.AddModelError("Code", "Invalid verification code.");
        return View(model);
    }

    private async Task<IActionResult> RedirectToDashboard()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login");

        // Enforce 2FA for Admins and SuperAdmins
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("SuperAdmin") || roles.Contains("Administrator"))
        {
            if (!user.TwoFactorEnabled)
            {
                return RedirectToAction("Enable2FA");
            }
        }

        if (roles.Contains("SuperAdmin"))
            return RedirectToAction("Index", "SuperAdmin");
        if (roles.Contains("Administrator"))
            return RedirectToAction("Index", "Admin");
        if (roles.Contains("ResearchAdviser"))
            return RedirectToAction("Index", "ResearchAdviser");
        if (roles.Contains("Student"))
            return RedirectToAction("Index", "Student");
        return RedirectToAction("Index", "Home");
    }
}
