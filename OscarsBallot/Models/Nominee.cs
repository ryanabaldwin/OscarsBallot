using System.ComponentModel.DataAnnotations;

namespace OscarsBallot.Models;

public class Nominee
{
    public int NomineeId { get; set; }
    public int CategoryId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public Category Category { get; set; } = null!;
    public ICollection<Ballot> Ballots { get; set; } = new List<Ballot>();
    public ICollection<Winner> WinningCategories { get; set; } = new List<Winner>();
}
