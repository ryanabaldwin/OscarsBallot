namespace OscarsBallot.ViewModels.Account;

public class AccountBallotViewModel
{
    public string UserDisplayName { get; set; } = string.Empty;
    public bool HasBallot { get; set; }
    public List<AccountBallotCategoryViewModel> Categories { get; set; } = [];
}

public class AccountBallotCategoryViewModel
{
    public string CategoryName { get; set; } = string.Empty;
    public string FirstChoiceName { get; set; } = string.Empty;
    public string SecondChoiceName { get; set; } = string.Empty;
}
