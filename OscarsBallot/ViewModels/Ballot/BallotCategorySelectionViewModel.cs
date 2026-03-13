using System.ComponentModel.DataAnnotations;

namespace OscarsBallot.ViewModels.Ballot;

public class BallotCategorySelectionViewModel
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public List<BallotNomineeOptionViewModel> Nominees { get; set; } = [];

    [Required]
    public int? FirstChoiceNomineeId { get; set; }

    [Required]
    public int? SecondChoiceNomineeId { get; set; }
}
