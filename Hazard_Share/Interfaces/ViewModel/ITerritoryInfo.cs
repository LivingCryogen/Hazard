using System.ComponentModel;

namespace Hazard_Share.Interfaces.ViewModel;
/// <summary>
/// Encapsulates data that should be public and updated for ViewModel/View bindings.
/// </summary>
/// <remarks>
/// Extending <see cref="INotifyPropertyChanged"/> and <see cref="INotifyPropertyChanging"/> to Territory data allows the use of MVVM Toolkit's ObservableProperty <br/>
/// and ObservableObject attributes.
/// </remarks>
public interface ITerritoryInfo : INotifyPropertyChanged, INotifyPropertyChanging
{
    /// <summary>
    /// Gets or sets the internal name of the Territory.
    /// </summary>
    /// <value>
    /// A <see cref="string"/>.
    /// </value>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the name of the Territory that should be displayed to the user.
    /// </summary>
    /// <value>
    /// A <see cref="string"/>.
    /// </value>
    public string DisplayName { get; set; }
    /// <summary>
    /// Gets or sets the number of the player who currently owns the territory.
    /// </summary>
    /// <value>
    /// An <see cref="int"/> between 0-5.
    /// </value>
    public int PlayerOwner { get; set; }
    /// <summary>
    /// Gets or sets the number of armies currently located within this territory.
    /// </summary>
    /// <value>
    /// An <see cref="int"/> >= 0.
    /// </value>
    public int Armies { get; set; }
    /// <summary>
    /// Gets or sets a flag indicating that this territory is currently selected by the user.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if it is currently selected; otherwise, <see langword="false"/>.
    /// </value>
    public bool IsSelected { get; set; }
}
