using System.ComponentModel;

namespace Common
{
    public class ObservableClass : INotifyPropertyChanged
    {
        public void OnPropertyChanged(string propertyName)
        {
            var pc = PropertyChanged;

            if (pc != null)
                pc(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
