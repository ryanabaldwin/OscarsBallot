namespace OscarsBallot.ViewModels.Admin;

public class AdminWinnersViewModel
{
    public List<AdminWinnerCategoryViewModel> Categories { get; set; } = [];
    public bool IsBallotEditingLocked { get; set; }
    public bool? BallotsLockedOverride { get; set; }
    public DateTime CeremonyStartMountain { get; set; }
}

public class AdminWinnerCategoryViewModel
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? SelectedWinnerNomineeId { get; set; }
    public List<AdminNomineeOptionViewModel> Nominees { get; set; } = [];
}

public class AdminNomineeOptionViewModel
{
    public int NomineeId { get; set; }
    public string Name { get; set; } = string.Empty;
}
