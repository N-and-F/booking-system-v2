namespace reservationSystem.Models.DTO;

public class ReportsTable
{
    public string Category { get; set; }
    public decimal In { get; set; }
    public decimal Out { get; set; }
    public decimal Profit { get; set; }
    public double ProfitMargin { get; set; }
}