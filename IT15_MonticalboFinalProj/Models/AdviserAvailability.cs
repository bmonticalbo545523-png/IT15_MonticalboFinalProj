namespace IT15_MonticalboFinalProj.Models;

public class AdviserAvailability
{
    public int Id { get; set; }

    // FK → Adviser
    public string AdviserId { get; set; } = string.Empty;
    public ApplicationUser? Adviser { get; set; }

    // Availability window
    public DateTime AvailableDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    // Whether this slot is already booked for a defense
    public bool IsBooked { get; set; } = false;
}
