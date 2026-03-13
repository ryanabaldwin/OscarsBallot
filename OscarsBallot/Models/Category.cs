using System.ComponentModel.DataAnnotations;

namespace OscarsBallot.Models;

public class Category
{
    public int CategoryId { get; set; }

    [Required]
    [MaxLength(200)]
    public string CategoryName { get; set; } = string.Empty;

    public decimal Points { get; set; }

    public ICollection<Nominee> Nominees { get; set; } = new List<Nominee>();
    public ICollection<Ballot> Ballots { get; set; } = new List<Ballot>();
    public Winner? Winner { get; set; }
}
