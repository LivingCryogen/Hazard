﻿using Shared.Geography.Enums;
using System.Windows;
using System.Windows.Input;

namespace View;

/// <summary>
/// Interaction logic for TroopAdvanceWindow.xaml
/// </summary>
public partial class TroopAdvanceWindow : Window
{
    private readonly TerrID _source;
    private readonly TerrID _target;
    private readonly int _minAdvance = 0;
    private readonly MainWindow? _parent;

    public TroopAdvanceWindow()
    {
        InitializeComponent();
    }
    public TroopAdvanceWindow(TerrID source, TerrID target, int min, int max, string message, MainWindow parentWindow)
    {
        InitializeComponent();
        _parent = parentWindow;

        MessageTextBlock.Text = message;
        for (int i = min; i <= max; i++)
            NumAdvanceBox.Items.Add(i);
        NumAdvanceBox.IsEditable = false;
        NumAdvanceBox.SelectedIndex = NumAdvanceBox.Items.Count - 1;
        NumAdvanceBox.Focus();

        _source = source;
        _target = target;
        _minAdvance = min;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        int numAdvance = NumAdvanceBox.SelectedIndex + _minAdvance;
        if (_parent != null)
            _parent.AdvanceParams = (_source, _target, numAdvance);
    }
    private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = true;
    }
    private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        this.Close();
    }
}
