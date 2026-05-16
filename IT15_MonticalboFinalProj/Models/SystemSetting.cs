namespace IT15_MonticalboFinalProj.Models;

public class SystemSetting
{
    public int Id { get; set; }
    public string SystemName { get; set; } = "Projexis";
    public string? LogoPath { get; set; }
    public string Theme { get; set; } = "light";
}
