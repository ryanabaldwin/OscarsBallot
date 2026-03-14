using System.ComponentModel.DataAnnotations;

namespace OscarsBallot.Models;

public class User
{
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(4)]
    public string Pin { get; set; } = string.Empty;

    public decimal Score { get; set; }

    public bool Admin { get; set; }

    public ICollection<Ballot> Ballots { get; set; } = new List<Ballot>();
}
