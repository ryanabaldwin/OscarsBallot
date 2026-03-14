namespace OscarsBallot.ViewModels.Ballot;

public class BallotSubmissionViewModel
{
    public List<BallotCategorySelectionViewModel> Categories { get; set; } = [];
    public bool IsEditingLocked { get; set; }
}
