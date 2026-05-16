using System.ComponentModel.DataAnnotations;

namespace IT15_MonticalboFinalProj.Models.ViewModels;

public class TwoFactorViewModel
{
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public class TwoFactorSetupViewModel
{
    public string SharedKey { get; set; } = string.Empty;
    public string QrCodeBase64 { get; set; } = string.Empty;
    
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string VerificationCode { get; set; } = string.Empty;
}
