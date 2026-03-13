using System.ComponentModel.DataAnnotations.Schema;

namespace OscarsBallot.Models;

public class Winner
{
    public int CategoryId { get; set; }

    [Column("Winner_Nominee_ID")]
    public int WinnerNomineeId { get; set; }

    public Category Category { get; set; } = null!;
    public Nominee WinnerNominee { get; set; } = null!;
}
