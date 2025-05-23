using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using View.Converters;

namespace View;

class TerritoryElement : ButtonBase
{
    private readonly SolidColorBrush _preselectColor = (SolidColorBrush)((App)Application.Current).FindResource("OceanHighlight");
    private bool _isPreSelected = false;

    public TerritoryElement() { }
    public TerritoryElement(int iD, string name)
    {
        ID = iD;
        Name = name;
        Geometry = Application.Current.FindResource(Name + "Geometry") as Geometry ?? Geometry.Empty;
        StationPosition = GetStationPosition(Geometry);

        Binding contentToSize = new("StationContent") {
            RelativeSource = RelativeSource.Self,
            NotifyOnSourceUpdated = true,
            Converter = new ArmiesTextToStation(),
            ConverterParameter = StationPosition
        };
        BindingOperations.SetBinding(this, StationProperty, contentToSize);

        Binding stationTextColor = new("Color") {
            RelativeSource = RelativeSource.Self,
            NotifyOnSourceUpdated = true
        };
        BindingOperations.SetBinding(this, StationTextColorProperty, stationTextColor);

        MouseEnter += OnMouseEnter;
        MouseLeave += OnMouseLeave;
    }

    public int ID { get; init; }
    public Point StationPosition { get; set; } = new(0, 0);

    public Geometry Geometry {
        get => (Geometry)GetValue(GeometryProperty);
        set { SetValue(GeometryProperty, value); }
    }
    public static readonly DependencyProperty? GeometryProperty = DependencyProperty.Register("Geometry", typeof(Geometry), typeof(TerritoryElement),
        new FrameworkPropertyMetadata(defaultValue: null, flags: FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange));

    public SolidColorBrush Color {
        get => (SolidColorBrush)GetValue(ColorProperty);
        set { SetValue(ColorProperty, value); }
    }
    public static readonly DependencyProperty? ColorProperty = DependencyProperty.Register("Color", typeof(SolidColorBrush), typeof(TerritoryElement),
        new FrameworkPropertyMetadata(defaultValue: Brushes.Transparent, flags: FrameworkPropertyMetadataOptions.AffectsRender));

    public Rect Station {
        get => (Rect)GetValue(StationProperty);
        set => SetValue(StationProperty, value);
    }
    public static readonly DependencyProperty? StationProperty = DependencyProperty.Register("Station", typeof(Rect), typeof(TerritoryElement),
        new FrameworkPropertyMetadata(defaultValue: new Rect(), flags: FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

    public Brush StationBackgroundColor {
        get => (Brush)GetValue(StationBackgroundColorProperty);
        set => SetValue(StationBackgroundColorProperty, value);
    }
    public static readonly DependencyProperty? StationBackgroundColorProperty = DependencyProperty.Register("StationBackgroundColor", typeof(Brush), typeof(TerritoryElement),
        new FrameworkPropertyMetadata(defaultValue: Brushes.WhiteSmoke, flags: FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush StationTextColor {
        get => (Brush)GetValue(StationTextColorProperty);
        set => SetValue(StationTextColorProperty, value);
    }
    public static readonly DependencyProperty? StationTextColorProperty = DependencyProperty.Register("StationTextColor", typeof(Brush), typeof(TerritoryElement),
        new FrameworkPropertyMetadata(defaultValue: Brushes.Black, flags: FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

    public string StationContent {
        get => (string)GetValue(StationContentProperty);
        set { SetValue(StationContentProperty, value); }
    }
    public static readonly DependencyProperty? StationContentProperty = DependencyProperty.Register("StationContent", typeof(string), typeof(TerritoryElement),
        new FrameworkPropertyMetadata(defaultValue: "0", flags: FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

    public bool IsSelected {
        get => (bool)GetValue(IsSelectedProperty);
        set { SetValue(IsSelectedProperty, value); }
    }
    public static readonly DependencyProperty? IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(TerritoryElement),
        new FrameworkPropertyMetadata(false, flags: FrameworkPropertyMetadataOptions.AffectsRender));

    #region Methods
    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        DrawTerritory(drawingContext);
        DrawStation(drawingContext);
        DrawStationText(drawingContext);
    }
    private protected Point GetStationPosition(Geometry territoryGeometry)
    {
        Point newPosition = new(0, 0);

        if (StationPosition.X == 0 && StationPosition.Y == 0)
            newPosition = FindSimpleStationPosition(territoryGeometry.Bounds, territoryGeometry, new Size(12, 21));

        if (newPosition.X == 0 && newPosition.Y == 0)
            MessageBox.Show("No Position found for Station at" + territoryGeometry.Bounds.Location.ToString() + "!");

        return newPosition;
    }

    private protected static Point FindSimpleStationPosition(Rect bounds, Geometry terrGeo, Size size)
    {
        Point testPoint = new(
            bounds.Left + bounds.Width / 2,
            bounds.Top + bounds.Height / 2);
        Point testPoint2 = GetOppositeVertexPoint(testPoint, size);
        if (GeometryFillContainsPoints(terrGeo, [testPoint, testPoint2]))
            return testPoint;

        testPoint = new(
            bounds.Left + bounds.Width / 3,
            bounds.Top + bounds.Height / 3);
        testPoint2 = GetOppositeVertexPoint(testPoint, size);
        if (GeometryFillContainsPoints(terrGeo, [testPoint, testPoint2]))
            return testPoint;

        testPoint = new(
            bounds.Left + bounds.Width / 3,
            bounds.Top + 2 * (bounds.Height / 3));
        testPoint2 = GetOppositeVertexPoint(testPoint, size);
        if (GeometryFillContainsPoints(terrGeo, [testPoint, testPoint2]))
            return testPoint;

        testPoint = new(
            bounds.Left + 2 * (bounds.Width / 3),
            bounds.Top + bounds.Height / 3);
        testPoint2 = GetOppositeVertexPoint(testPoint, size);
        if (GeometryFillContainsPoints(terrGeo, [testPoint, testPoint2]))
            return testPoint;

        testPoint = new(
            bounds.Left + 2 * (bounds.Width / 3),
            bounds.Top + 2 * (bounds.Height / 3));
        testPoint2 = GetOppositeVertexPoint(testPoint, size);
        if (GeometryFillContainsPoints(terrGeo, [testPoint, testPoint2]))
            return testPoint;

        testPoint = new(
            bounds.Left + bounds.Width / 3,
            bounds.Top + bounds.Height / 2);
        testPoint2 = GetOppositeVertexPoint(testPoint, size);
        if (GeometryFillContainsPoints(terrGeo, [testPoint, testPoint2]))
            return testPoint;

        testPoint = new(
            bounds.Left + 2 * (bounds.Width / 3),
            bounds.Top + bounds.Height / 2);
        testPoint2 = GetOppositeVertexPoint(testPoint, size);
        if (GeometryFillContainsPoints(terrGeo, [testPoint, testPoint2]))
            return testPoint;

        testPoint = new(
            bounds.Left + bounds.Width / 2,
            bounds.Top + bounds.Height / 2);
        testPoint2 = new(
            testPoint.Y,
            testPoint.X + size.Width);
        if (GeometryFillContainsPoints(terrGeo, [testPoint, testPoint2]))
            return testPoint;

        return new(
            bounds.Left + bounds.Width / 2,
            bounds.Top + bounds.Height / 2);
    }
    private static Point GetOppositeVertexPoint(Point origin, Size size)
    {
        return new(origin.Y + size.Height, origin.X + size.Width);
    }
    private static bool GeometryFillContainsPoints(Geometry geometry, Point[] points)
    {
        foreach (Point point in points)
            if (!geometry.FillContains(point))
                return false;
        return true;
    }

    private protected void DrawTerritory(DrawingContext drawingContext)
    {
        if (Geometry == null)
            return;
        if (_isPreSelected)
            drawingContext.DrawGeometry(_preselectColor, new Pen(Color, .5), Geometry);
        else
            drawingContext.DrawGeometry(Color, new Pen(Color, .5), Geometry);
    }
    private protected void DrawStation(DrawingContext drawingContext)
    {
        if (!Station.Size.IsEmpty)
            drawingContext.DrawRoundedRectangle(
                StationBackgroundColor, new Pen(StationBackgroundColor, 1), Station, 2.5, 2.5);
    }
    private protected void DrawStationText(DrawingContext drawingContext)
    {
        if (StationContent == null)
            return;
        FormattedText drawText = new(
            StationContent, new("en-us"), FlowDirection.LeftToRight, new("Courier New"), 19, StationTextColor, 50);
        drawingContext.DrawText(drawText, Station.TopLeft);
    }
    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (Command.CanExecute(ID)) {
            _isPreSelected = true;
            InvalidateVisual();
        }
    }
    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (_isPreSelected) {
            _isPreSelected = false;
            InvalidateVisual();
        }
    }
    #endregion
}
