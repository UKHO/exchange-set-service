namespace UKHO.SalesCatalogueFileShareServicesMock.API.Data.FileShareService
{
    public enum BatchStatus
    {
        Incomplete = 1,
        CommitInProgress = 2,
        Committed = 3,
        Rolledback = 4,
        Failed = 5,
        Deleted = 6
    }
}
