namespace Species.Client.Models;

public sealed class PolityPressureItem
{
    public PolityPressureItem(string label, int value, string severityLabel)
    {
        Label = label;
        Value = value;
        SeverityLabel = severityLabel;
    }

    public string Label { get; }

    public int Value { get; }

    public string SeverityLabel { get; }
}
