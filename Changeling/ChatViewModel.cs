using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using MindFlayer;

namespace Changeling
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<ChatMessage> chatMessages = new ObservableCollection<ChatMessage>();

        public ChatViewModel()
        {
            ChatMessages.Add(new ChatMessage { Role = "system", Content = "You are a helpful assistant." });
        }

        public ObservableCollection<ChatMessage> ChatMessages
        {
            get { return chatMessages; }
            set
            {
                chatMessages = value;
                OnPropertyChanged(nameof(ChatMessages));
            }
        }

        private string newMessageContent;

        public string NewMessageContent
        {
            get { return newMessageContent; }
            set
            {
                newMessageContent = value;
                OnPropertyChanged(nameof(NewMessageContent));
            }
        }

        private ICommand sendMessageCommand;

        public ICommand SendMessageCommand
        {
            get
            {
                if (sendMessageCommand == null)
                {
                    sendMessageCommand = new RelayCommand(() => true, SendMessage);
                }

                return sendMessageCommand;
            }
        }

        private void SendMessage()
        {
            ChatMessages.Add(new ChatMessage { Role = "user", Content = NewMessageContent });
            // (Assuming some logic here to process the message and send a response.)
            ChatMessages.Add(new ChatMessage { Role = "assistant", Content = Engine.Chat(NewMessageContent, chatMessages) });
            NewMessageContent = string.Empty;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
