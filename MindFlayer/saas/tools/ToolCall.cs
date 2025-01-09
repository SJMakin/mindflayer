using System.ComponentModel;
using System.Text.Json.Serialization;

namespace MindFlayer.saas.tools
{
    // Helper classes for tool information
    public class ToolCall : INotifyPropertyChanged
    {
        public string ID { get; set; } = "";
        public string Name { get; set; } = "";
        public string Parameters { get; set; } = "";

        [JsonIgnore]
        public bool IsLoaded = false;

        private string _result;
        public string Result
        {
            get => _result;
            set
            {
                _result = value;
                OnPropertyChanged(nameof(Result));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
