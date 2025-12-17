using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace General_PCR18.Lang
{
    public class RService : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private static readonly RService _current = new RService();
        public static RService Current => _current;

        private readonly Properties.Resources resource = new Properties.Resources();
        public Properties.Resources Res => resource;

        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ChangedCulture(string name)
        {
            Properties.Resources.Culture = CultureInfo.GetCultureInfo(name);
            this.RaisePropertyChanged(nameof(Res));
        }
    }
}
