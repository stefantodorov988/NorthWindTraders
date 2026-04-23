namespace NorthWindTraders.Domain.Customers;

/// <summary>
/// Customer aggregate root (identity + name fields used for search and display).
/// Persistence-agnostic: maps from rows or documents in infrastructure, never exposed as an EF entity from API.
/// </summary>
public sealed class Customer
{
    public Customer(
        CustomerId id,
        string companyName,
        string? contactName,
        string? contactTitle)
    {
        Id = id;
        CompanyName = companyName ?? throw new ArgumentNullException(nameof(companyName));
        ContactName = contactName;
        ContactTitle = contactTitle;
    }

    public CustomerId Id { get; }
    public string CompanyName { get; }
    public string? ContactName { get; }
    public string? ContactTitle { get; }

    /// <summary>
    /// All textual name fields participate: company, contact name, contact title (per assessment clarification).
    /// </summary>
    public bool MatchesNameSearch(string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return true;

        var term = searchTerm.Trim();
        return NameFieldContains(CompanyName, term)
               || NameFieldContains(ContactName, term)
               || NameFieldContains(ContactTitle, term);
    }

    private static bool NameFieldContains(string? field, string term) =>
        field is not null && field.Contains(term, StringComparison.OrdinalIgnoreCase);
}
