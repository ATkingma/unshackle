namespace Unshackle.Core.Fixes.EPPlus.Patterns;

// The layout statements of LayoutPattern (queue row 9).
public enum LayoutShape
{
    // range.Merge = ...;
    Merge,

    // sheet.Row(i).Height = ...;
    Height,

    // sheet.Column(i).Width = ...;
    Width,

    // sheet.View.FreezePanes(row, column);
    FreezePanes,

    // range.AutoFitColumns(...); / sheet.Column(i).AutoFit(...);
    AutoFit,
}
