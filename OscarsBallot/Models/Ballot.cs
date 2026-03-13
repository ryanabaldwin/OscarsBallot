namespace OscarsBallot.Models;

public class Ballot
{
    public int BallotId { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public int Rank { get; set; }
    public int NomineeId { get; set; }

    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Nominee Nominee { get; set; } = null!;
}
