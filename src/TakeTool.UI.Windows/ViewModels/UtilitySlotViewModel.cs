using CommunityToolkit.Mvvm.ComponentModel;
using TakeTool.Core.Abstractions;

namespace TakeTool.UI.Windows.ViewModels;

public partial class UtilitySlotViewModel : ObservableObject
{
    public UtilitySlotViewModel(IUtility utility, double angleDegrees, double radius)
    {
        Utility = utility;
        AngleDegrees = angleDegrees;
        Radius = radius;
        RecalculateOffset();
    }

    public IUtility Utility { get; }

    public string Name => Utility.Metadata.Name;

    public string IconKey => Utility.Metadata.IconKey;

    public double AngleDegrees { get; }

    public double Radius { get; }

    [ObservableProperty]
    private double offsetX;

    [ObservableProperty]
    private double offsetY;

    [ObservableProperty]
    private bool isHighlighted;

    public void RecalculateOffset()
    {
        var radians = AngleDegrees * Math.PI / 180.0;
        // Arc opens upward-left from bottom-right hub.
        OffsetX = -Math.Cos(radians) * Radius;
        OffsetY = -Math.Sin(radians) * Radius;
    }

    public bool CanAcceptDrop(IReadOnlyList<string> files)
    {
        var context = new UtilityContext
        {
            FilePaths = files,
            Trigger = UtilityTrigger.Drop
        };
        return Utility.CanAccept(context);
    }
}
