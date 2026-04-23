namespace NorthWindTraders.Domain.Customers;

/// <summary>
/// Northwind customer identifier (e.g. ALFKI). Not tied to any ORM key type.
/// </summary>
public readonly record struct CustomerId
{
    public string Value { get; private init; }

    public static CustomerId From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Customer id is required.", nameof(value));

        return new CustomerId { Value = value.Trim() };
    }

    public override string ToString() => Value;
}
