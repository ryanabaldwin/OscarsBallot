namespace OscarsBallot.ViewModels.Leaderboard;

public class LeaderboardEntryViewModel
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public decimal Score { get; set; }
}
