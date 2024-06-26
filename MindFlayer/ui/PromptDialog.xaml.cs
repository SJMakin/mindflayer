﻿using System.Windows;

namespace MindFlayer.ui;

public partial class PromptDialog : Window
{
    public string PromptResult { get; private set; }

    public PromptDialog(string prompt)
    {
        InitializeComponent();
        PromptTextBox.Text = prompt;
    }

    private void PromptButton_Click(object sender, RoutedEventArgs e)
    {
        PromptTextBox.Text = ((System.Windows.Controls.Button)sender).Tag?.ToString();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        PromptResult = PromptTextBox.Text;
        this.DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
    }
}