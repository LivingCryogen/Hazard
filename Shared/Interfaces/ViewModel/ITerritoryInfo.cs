﻿using System.ComponentModel;

namespace Shared.Interfaces.ViewModel;
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
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the name of the Territory that should be displayed to the user.
    /// </summary>
    public string DisplayName { get; set; }
    /// <summary>
    /// Gets or sets the number of the player who currently owns the territory.
    /// </summary>
    /// <value>
    /// 0-5.
    /// </value>
    public int PlayerOwner { get; set; }
    /// <summary>
    /// Gets or sets the number of armies currently located within this territory.
    /// </summary>
    public int Armies { get; set; }
    /// <summary>
    /// Gets or sets a flag indicating that this territory is currently selected by the user.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if it is currently selected; otherwise, <see langword="false"/>.
    /// </value>
    public bool IsSelected { get; set; }
}
