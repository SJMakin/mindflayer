﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using MindFlayer.saas;

namespace MindFlayer;

public class Conversation : INotifyPropertyChanged
{
    private ObservableCollection<ChatMessage> _chatMessages = new ObservableCollection<ChatMessage>();
    private string _name;
    private readonly ChatViewModel _parent;

    public Conversation(ChatViewModel parent)
    {
        this._parent = parent;
        ShowCloseButton = Visibility.Visible;
    }

    public Conversation()
    {
        ShowCloseButton = Visibility.Hidden;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public Visibility ShowCloseButton { get; }

    public ObservableCollection<ChatMessage> ChatMessages
    {
        get => _chatMessages;
        set
        {
            _chatMessages = value;
            OnPropertyChanged(nameof(ChatMessages));
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged(nameof(Name));
        }
    }

    public int? TokenCount { get; set; }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private ICommand _closeTabCommand;
    public ICommand CloseTabCommand => _closeTabCommand ??= new RelayCommand(() => true, CloseTab);

    private void CloseTab()
    {
        if (_parent.Conversations.Count <= 2) return;
        _parent.Removing = true;
        _parent.Conversations.Remove(this);
        _parent.Removing = false;
    }


    private ICommand _saveCommand;
    public ICommand SaveCommand => _saveCommand ??= new RelayCommand(() => true, Save);

    private void Save()
    {
        using var sfd = new SaveFileDialog();
        sfd.Filter = "Conversation Files (*.convo)|*.convo";
        var result = sfd.ShowDialog();
        if (result != DialogResult.OK) return;
        var convo = JsonSerializer.Serialize(ChatMessages.ToList(), new JsonSerializerOptions() {WriteIndented = true});
        File.WriteAllText(sfd.FileName, convo);
    }

    private ICommand _loadCommand;
    public ICommand LoadCommand => _loadCommand ??= new RelayCommand(() => true, Load);

    private void Load()
    {
        using var sfd = new OpenFileDialog();
        sfd.Filter = "Conversation Files (*.convo)|*.convo";
        var result = sfd.ShowDialog();
        if (result != DialogResult.OK) return;
        var convoText = File.ReadAllText(sfd.FileName);
        var convoMessages = JsonSerializer.Deserialize<ObservableCollection<ChatMessage>>(convoText);
        var convo = new Conversation(_parent) { ChatMessages = convoMessages, Name = Path.GetFileNameWithoutExtension(sfd.FileName) };
        foreach (var msg in convo.ChatMessages)
        {
            msg.Parent = convo;
        }
        _parent.Conversations.Insert(_parent.Conversations.Count - 1, convo);
        
    }

    public void ReplayFromThisMessage(ChatMessage message)
    {
        int startIndex = ChatMessages.IndexOf(message);
        if (startIndex >= 0 && startIndex < ChatMessages.Count)
        {
            int count = ChatMessages.Count - startIndex;
            for (int i = 0; i < count; i++)
            {
                ChatMessages.RemoveAt(startIndex);
            }
        }

        var msg = new ChatMessage(this)
        {
            Role = OpenAI.Chat.Role.Assistant,
            Content = ""
        };

        ChatMessages.Add(msg);

        Task.Run(() => ApiWrapper.ChatStream(ChatMessages, _parent.Temperature, (t) =>
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (t == null) return;
                msg.Content = msg.Content + t;

            });
        }, _parent.SelectedChatModel));
    }
    
}
