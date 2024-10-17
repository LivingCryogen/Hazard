using Hazard.ViewModel.SubElements;
using Share.Enums;
using Share.Interfaces.ViewModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace View;

/// <summary>
/// Interaction logic for AttackWindow.xaml
/// </summary>
public partial class AttackWindow : Window
{
    private enum SpinType : int
    {
        Null = 0,
        roll03 = 1,
        roll03b = 2,
        roll03c = 3,
        roll05 = 4,
        roll05b = 5,
        roll05c = 6,
        roll06 = 7,
        roll06b = 8,
        roll06c = 9,
        roll08 = 10
    }
    private enum DiceGroup : int // a "troolean" used later for checking which dice groups' visibility to update after dice thrown
    {
        Attack = -1,
        All = 0,
        Defense = 1
    }

    private IMainVM? _vM = null;
    private readonly ObjectAnimationUsingKeyFrames[] _attackDiceAnimations = new ObjectAnimationUsingKeyFrames[3];
    private readonly ObjectAnimationUsingKeyFrames[] _defenseDiceAnimations = new ObjectAnimationUsingKeyFrames[2];
    private readonly Storyboard? _lossIndicatorActiveAnimation;
    private readonly Image[] _attackDiceVisuals = new Image[3];
    private readonly Image[] _defenseDiceVisuals = new Image[2];
    private Geometry? _targetGeometry = null;
    private Geometry? _sourceGeometry = null;
    private SpinType _currentSpinType = SpinType.Null;
    private int _oldSourceArmies = 0;
    private int _oldTargetArmies = 0;
    private SolidColorBrush? _sourceColor = null;
    private SolidColorBrush? _targetColor = null;
    private bool _sourceArmiesChanged = false;
    private bool _targetArmiesChanged = false;
    private bool _attackFails = false;
    private readonly BitmapImage[] AttackDieFaces = [
        (BitmapImage)Application.Current.FindResource("AttackDie1"),
        (BitmapImage)Application.Current.FindResource("AttackDie2"),
        (BitmapImage)Application.Current.FindResource("AttackDie3"),
        (BitmapImage)Application.Current.FindResource("AttackDie4"),
        (BitmapImage)Application.Current.FindResource("AttackDie5"),
        (BitmapImage)Application.Current.FindResource("AttackDie6")
    ];
    private readonly BitmapImage[] DefenseDieFaces = [
        (BitmapImage)Application.Current.FindResource("DefenseDie1"),
        (BitmapImage)Application.Current.FindResource("DefenseDie2"),
        (BitmapImage)Application.Current.FindResource("DefenseDie3"),
        (BitmapImage)Application.Current.FindResource("DefenseDie4"),
        (BitmapImage)Application.Current.FindResource("DefenseDie5"),
        (BitmapImage)Application.Current.FindResource("DefenseDie6")
    ];

    public AttackWindow()
    {
        InitializeComponent();

        _attackDiceVisuals[0] = AttackDieVisual1;
        _attackDiceVisuals[1] = AttackDieVisual2;
        _attackDiceVisuals[2] = AttackDieVisual3;
        _defenseDiceVisuals[0] = DefenseDieVisual1;
        _defenseDiceVisuals[1] = DefenseDieVisual2;
        _lossIndicatorActiveAnimation = FindResource("LossIndicatorActiveAnimation") as Storyboard;

        for (int i = 0; i < 3; i++) {
            _attackDiceVisuals[i].Visibility = Visibility.Hidden;
            if (i < 2)
                _defenseDiceVisuals[i].Visibility = Visibility.Hidden;
        }

        Binding attackEnabled = new("AttackEnabled") {
            NotifyOnSourceUpdated = true,
            Mode = BindingMode.OneWay
        };
        BindingOperations.SetBinding(this, AttackEnabledProperty, attackEnabled);

        Closing += OnWindowClosing;

        // The following adds notifications to this window when its AttackEnabled property (bound to the VM's version) changes
        // This should, then, allow automatic refocus of the ConfirmAttackButton once it is re-enabled
        DependencyPropertyDescriptor attackEnabledDescriptor = DependencyPropertyDescriptor.FromProperty(AttackEnabledProperty, typeof(AttackWindow));
        attackEnabledDescriptor?.AddValueChanged(this, OnAttackEnableChanged);

        ConfirmAttackButton.Focus();
    }

    #region Properties
    public int MaxAttackDice => CalcMaxAttackDice();
    public int MaxDefenseDice => CalcMaxDefenseDice();
    public int NumAttackDice {
        get { return (int)GetValue(NumAttackDiceProperty); }
        set { SetValue(NumAttackDiceProperty, value); }
    }
    public bool AttackEnabled {
        get { return (bool)GetValue(AttackEnabledProperty); }
        set { SetValue(AttackEnabledProperty, value); }
    }
    #endregion
    // Using a DependencyProperty as the backing store for AttackEnabled.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty AttackEnabledProperty =
        DependencyProperty.Register("AttackEnabled", typeof(bool), typeof(AttackWindow), new PropertyMetadata(defaultValue: false));



    // Using a DependencyProperty as the backing store for AttackDice.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty NumAttackDiceProperty =
        DependencyProperty.Register("NumAttackDice", typeof(int), typeof(AttackWindow), new PropertyMetadata(defaultValue: 0));

    public int NumDefenseDice {
        get { return (int)GetValue(NumDefenseDiceProperty); }
        set { SetValue(NumDefenseDiceProperty, value); }
    }

    // Using a DependencyProperty as the backing store for DefenseDice.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty NumDefenseDiceProperty =
        DependencyProperty.Register("NumDefenseDice", typeof(int), typeof(AttackWindow), new PropertyMetadata(defaultValue: 0));

    public int[] AttackParams {
        get { return (int[])GetValue(AttackParamsProperty); }
        set { SetValue(AttackParamsProperty, value); }
    }

    // Using a DependencyProperty as the backing store for AttackParams.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty AttackParamsProperty =
        DependencyProperty.Register("AttackParams", typeof(int[]), typeof(AttackWindow), new PropertyMetadata(defaultValue: new int[4] { 0, 0, 0, 0 }));

    public string TargetName {
        get { return (string)GetValue(TargetNameProperty); }
        set { SetValue(TargetNameProperty, value); }
    }

    // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TargetNameProperty =
        DependencyProperty.Register("TargetName", typeof(string), typeof(AttackWindow), new PropertyMetadata(defaultValue: string.Empty));

    public string SourceName {
        get { return (string)GetValue(SourceNameProperty); }
        set { SetValue(SourceNameProperty, value); }
    }

    // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SourceNameProperty =
        DependencyProperty.Register("SourceName", typeof(string), typeof(AttackWindow), new PropertyMetadata(defaultValue: string.Empty));

    public int SourceArmies {
        get { return (int)GetValue(SourceArmiesProperty); }
        set { SetValue(SourceArmiesProperty, value); }
    }

    // Using a DependencyProperty as the backing store for SourceArmies.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SourceArmiesProperty =
        DependencyProperty.Register("SourceArmies", typeof(int), typeof(AttackWindow), new PropertyMetadata(defaultValue: 0));

    public int TargetArmies {
        get { return (int)GetValue(TargetArmiesProperty); }
        set { SetValue(TargetArmiesProperty, value); }
    }

    // Using a DependencyProperty as the backing store for TargetArmies.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TargetArmiesProperty =
        DependencyProperty.Register("TargetArmies", typeof(int), typeof(AttackWindow), new PropertyMetadata(defaultValue: 0));

    public int SourceLoss {
        get { return (int)GetValue(SourceLossProperty); }
        set { SetValue(SourceLossProperty, value); }
    }

    // Using a DependencyProperty as the backing store for SourceLoss.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SourceLossProperty =
        DependencyProperty.Register("SourceLoss", typeof(int), typeof(AttackWindow), new PropertyMetadata(defaultValue: 0));


    public int TargetLoss {
        get { return (int)GetValue(TargetLossProperty); }
        set { SetValue(TargetLossProperty, value); }
    }

    // Using a DependencyProperty as the backing store for TargetLoss.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TargetLossProperty =
        DependencyProperty.Register("TargetLoss", typeof(int), typeof(AttackWindow), new PropertyMetadata(defaultValue: 0));

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // randomize starting die visuals 
        Random rand = new();
        for (int i = 0; i < 3; i++) {
            _attackDiceVisuals[i].Source = AttackDieFaces[rand.Next(1, 6)];
            if (i < 2)
                _defenseDiceVisuals[i].Source = DefenseDieFaces[rand.Next(1, 6)];
        }
    }

    public void Initialize(int source, int target, SolidColorBrush sourceColor, SolidColorBrush targetColor, IMainVM vM)
    {
        DataContext = vM;
        _vM = vM;

        SourceName = _vM.Territories[source].DisplayName;
        TargetName = _vM.Territories[target].DisplayName;

        Binding sourceArmiesText = new("Territories[" + source.ToString() + "].ArmiesText") {
            NotifyOnSourceUpdated = true,
            NotifyOnTargetUpdated = true
        };
        BindingOperations.SetBinding(SourceArmiesBlock, TextBlock.TextProperty, sourceArmiesText);
        Binding targetArmiesText = new("Territories[" + target.ToString() + "].ArmiesText") {
            NotifyOnSourceUpdated = true,
            NotifyOnTargetUpdated = true
        };
        BindingOperations.SetBinding(TargetArmiesBlock, TextBlock.TextProperty, targetArmiesText);

        _vM.Territories[source].PropertyChanging += OnSourceChanging;
        _vM.Territories[target].PropertyChanging += OnTargetChanging;
        _vM.Territories[source].PropertyChanged += OnSourceChanged;
        _vM.Territories[target].PropertyChanged += OnTargetChanged;

        _sourceGeometry = Application.Current.FindResource(_vM!.Territories![source].Name + "Geometry") as Geometry;
        _targetGeometry = Application.Current.FindResource(_vM!.Territories![target].Name + "Geometry") as Geometry;
        _sourceColor = sourceColor;
        _targetColor = targetColor;

        SourceTerritoryImage.Data = _sourceGeometry;
        SourceTerritoryImage.Fill = _sourceColor;
        TargetTerritoryImage.Data = _targetGeometry;
        TargetTerritoryImage.Fill = _targetColor;

        SourceNameBlock.Foreground = _sourceColor;
        TargetNameBlock.Foreground = _targetColor;
        SourceArmiesBlock.Foreground = _sourceColor;
        TargetArmiesBlock.Foreground = _targetColor;

        this.SizeToContent = SizeToContent.WidthAndHeight;

        SourceArmies = _vM!.Territories[source].Armies;
        TargetArmies = _vM!.Territories[target].Armies;
        Binding sourceArmies = new("Territories[" + source.ToString() + "].Armies") {
            NotifyOnSourceUpdated = true,
            NotifyOnTargetUpdated = true
        };
        BindingOperations.SetBinding(this, SourceArmiesProperty, sourceArmies);
        Binding targetArmies = new("Territories[" + target.ToString() + "].Armies") {
            NotifyOnSourceUpdated = true,
            NotifyOnTargetUpdated = true
        };
        BindingOperations.SetBinding(this, TargetArmiesProperty, targetArmies);

        if (SourceArmies >= 4)
            NumAttackDice = 3;
        else if (SourceArmies == 3)
            NumAttackDice = 2;
        else
            NumAttackDice = 1;

        if (TargetArmies >= 2)
            NumDefenseDice = 2;
        else
            NumDefenseDice = 1;

        AttackParams[0] = source;
        AttackParams[1] = target;
        AttackParams[2] = NumAttackDice;
        AttackParams[3] = NumDefenseDice;

        for (int i = 0; i < NumAttackDice; i++) {
            _attackDiceVisuals[i].Source = AttackDieFaces[i];
        }

        for (int i = 0; i < NumDefenseDice; i++) {
            _defenseDiceVisuals[i].Source = DefenseDieFaces[i];
        }

        SetDiceVisibility(DiceGroup.All);

        _vM.DiceThrown += HandleDiceThrown;
    }

    private void OnSourceChanging(object? sender, PropertyChangingEventArgs e)
    {
        if (sender is not TerritoryInfo territory) return;
        if (e.PropertyName == "Armies")
            _oldSourceArmies = territory.Armies;
    }

    private void OnTargetChanging(object? sender, PropertyChangingEventArgs e)
    {
        if (sender is TerritoryInfo territory)
            if (e.PropertyName == "Armies")
                _oldTargetArmies = territory.Armies;
    }

    private void OnSourceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender != null) {
            var territory = sender as TerritoryInfo;
            if (e.PropertyName == "Armies") {
                SourceLoss = _oldSourceArmies - territory!.Armies;
                if (territory.Armies < 2) {
                    _attackFails = true;
                }
                else {
                    UpdateDiceCount();
                    SetDiceVisibility(DiceGroup.Attack);
                }
                _sourceArmiesChanged = true;
            }
        }
    }

    private void OnTargetChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TerritoryInfo territory) return;

        if (e.PropertyName == "Armies") {
            TargetLoss = _oldTargetArmies - territory.Armies;
            UpdateDiceCount();
            SetDiceVisibility(DiceGroup.Defense);
            _targetArmiesChanged = true;
        }
        else if (e.PropertyName == "PlayerOwner") {
            TargetArmiesBlock.Foreground = _sourceColor;
            TargetNameBlock.Foreground = _sourceColor;
            TargetTerritoryImage.Fill = _sourceColor;
        }
    }

    private void HandleDiceThrown(object? sender, IDiceThrownEventArgs results)
    {
        int numAttackDice = results.AttackResults.Count;
        int numDefenseDice = results.DefenseResults.Count;

        _currentSpinType = DetermineSpin();
        int tenthSeconds = _currentSpinType.ToString().ElementAt(5) - '0';
        long spinTicks = tenthSeconds * 1000000; // A second includes 10,000,000 ticks. Each diceroll sound file duration lasts tenths of seconds, so tick conversion needs * 1,000,000. 

        for (int i = 0; i < numAttackDice; i++)
            _attackDiceAnimations[i] = BuildDiceAnimation(AttackDieFaces, spinTicks, results.AttackResults[i]);

        for (int i = 0; i < numDefenseDice; i++)
            _defenseDiceAnimations[i] = BuildDiceAnimation(DefenseDieFaces, spinTicks, results.DefenseResults[i]);

        SetDiceVisibility(DiceGroup.All);
        BeginDiceAnimations();
        PlayDiceSound(_currentSpinType);

        if (!_sourceArmiesChanged)
            SourceLoss = 0;
        if (!_targetArmiesChanged)
            TargetLoss = 0;

        _lossIndicatorActiveAnimation?.Begin();

        if (_attackFails) {
            MessageBox.Show("Our attack failed! We must retreat!");
            Close();
        }

        _targetArmiesChanged = false;
        _sourceArmiesChanged = false;
    }

    private void UpdateDiceCount()
    {
        if (NumAttackDice > MaxAttackDice)
            NumAttackDice = MaxAttackDice;
        if (NumDefenseDice > MaxDefenseDice)
            NumDefenseDice = MaxDefenseDice;

        AttackParams[2] = NumAttackDice;
        AttackParams[3] = NumDefenseDice;
    }

    private void SetDiceVisibility(DiceGroup diceGroup)
    {
        switch (diceGroup) {
            case DiceGroup.Attack:
                DisplayAttackDice();
                break;
            case DiceGroup.Defense:
                DisplayDefenseDice();
                break;
            case DiceGroup.All:
                DisplayAttackDice();
                DisplayDefenseDice();
                break;
        }
    }

    private void DisplayAttackDice()
    {
        for (int i = 0; i < NumAttackDice; i++)
            _attackDiceVisuals[i].Visibility = Visibility.Visible;

        for (int i = 2; i >= NumAttackDice; i--)
            _attackDiceVisuals[i].Visibility = Visibility.Hidden;
    }

    private void DisplayDefenseDice()
    {
        for (int i = 0; i < NumDefenseDice; i++)
            _defenseDiceVisuals[i].Visibility = Visibility.Visible;

        if (NumDefenseDice == 1)
            _defenseDiceVisuals[1].Visibility = Visibility.Hidden;
    }

    private void BeginDiceAnimations()
    {
        if (NumAttackDice == 3)
            AttackDieVisual3.BeginAnimation(Image.SourceProperty, _attackDiceAnimations[2]);
        if (NumAttackDice >= 2)
            AttackDieVisual2.BeginAnimation(Image.SourceProperty, _attackDiceAnimations[1]);
        if (NumAttackDice >= 1)
            AttackDieVisual1.BeginAnimation(Image.SourceProperty, _attackDiceAnimations[0]);

        if (NumDefenseDice == 2) {
            DefenseDieVisual2.BeginAnimation(Image.SourceProperty, _defenseDiceAnimations[1]);
            DefenseDieVisual1.BeginAnimation(Image.SourceProperty, _defenseDiceAnimations[0]);
        }
        else
            DefenseDieVisual1.BeginAnimation(Image.SourceProperty, _defenseDiceAnimations[0]);
    }

    private static SpinType DetermineSpin()
    {
        Random rand = new();
        int randType = rand.Next(1, 10);
        return (SpinType)randType;
    }

    private static void PlayDiceSound(SpinType spinType)
    {
        MediaPlayer dicePlayer = new();
        dicePlayer.Open(new(spinType.ToString() + ".wav", UriKind.Relative));
        dicePlayer.Volume = .5;
        dicePlayer.Play();
    }

    private static ObjectAnimationUsingKeyFrames BuildDiceAnimation(BitmapImage[] diceImages, long tickDuration, int result)
    {
        ObjectKeyFrameCollection diceFrames = [];

        int[] faces = GenerateDieFaces(tickDuration);

        for (int i = 0; i < faces.Length - 1; i++) // last int in the array must be excluded here (it's the number of spins, not a die face index)
            diceFrames.Add(new DiscreteObjectKeyFrame(diceImages[faces[i]], KeyTime.Paced));

        diceFrames.Add(new DiscreteObjectKeyFrame(diceImages[result], KeyTime.Paced));

        ObjectAnimationUsingKeyFrames dieAnimation = new() {
            KeyFrames = diceFrames,
            Duration = new TimeSpan(tickDuration),
            DecelerationRatio = .5,
            AccelerationRatio = .5
        };
        return dieAnimation;
    }

    private static int[] GenerateDieFaces(long tickDuration) // last returned int specifies number of spins
    {
        int maxSpins = tickDuration.ToString().First() - '0';
        Random rand = new();
        int spins = rand.Next(3, maxSpins);
        List<int> faces = [];

        int facesThisSpin;
        for (int i = 0; i < spins; i++) {
            facesThisSpin = rand.Next(3, 11);
            int faceShowing = 0;
            for (int j = 0; j < facesThisSpin; j++) {
                int newFace;
                do {
                    newFace = rand.Next(1, 6);
                }
                while (newFace == faceShowing);

                faces.Add(newFace);
                faceShowing = newFace;
            }
        }
        faces.Add(spins);
        return [.. faces];
    }


    private void SourceDiceUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (NumAttackDice < MaxAttackDice) {
            NumAttackDice++;
            AttackParams[2] = NumAttackDice; // Command Parameter for Attack Command include number of Attack Dice as final element and must be manually updated

            SetDiceVisibility(DiceGroup.Attack);
        }
    }

    private void SourceDiceDownButton_Click(object sender, RoutedEventArgs e)
    {
        if (NumAttackDice > 1) {
            NumAttackDice--;
            AttackParams[2] = NumAttackDice; // Command Parameter for Attack Command include number of Attack Dice as final element and must be manually updated

            SetDiceVisibility(DiceGroup.Attack);
        }
    }

    public int CalcMaxAttackDice()
    {
        int max = 0;
        if (SourceArmies >= 4)
            max = 3;
        else if (SourceArmies == 3)
            max = 2;
        else if (SourceArmies == 2)
            max = 1;

        return max;
    }

    public int CalcMaxDefenseDice()
    {
        int max = 0;

        if (TargetArmies >= 2)
            max = 2;
        else if (TargetArmies == 1)
            max = 1;

        return max;
    }

    private void CloseAttackWindowButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private protected void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_vM!.Territories != null && _vM.TerritorySelected != TerrID.Null)
            _vM.Territories[(int)_vM.TerritorySelected].IsSelected = false;

        _vM.TerritorySelected = TerrID.Null;

        _vM.DiceThrown -= HandleDiceThrown;
    }

    private void CommandBinding_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
    {
        if (_vM?.AttackEnabled ?? false) {
            e.CanExecute = true;
        }
        else
            e.CanExecute = false;
    }

    private void OnAttackEnableChanged(object? sender, EventArgs e)
    {
        if (AttackEnabled)
            ConfirmAttackButton.Focus();
    }

    private void CommandBinding_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
    {
        this.Close();
    }
}

