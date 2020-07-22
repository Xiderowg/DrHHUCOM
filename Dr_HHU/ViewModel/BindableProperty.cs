using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Threading;
using System.Windows;

namespace Dr_HHU.ViewModel
{
    public class BindableProperty : INotifyPropertyChanged
    {
        public BindableProperty()
        {
        }

        public void UpdateProper<T>(ref T properValue, T newValue, [CallerMemberName] string properName = "")
        {
            if (Equals(properValue, newValue))
            {
                return;
            }

            properValue = newValue;
            OnPropertyChanged(properName);
        }

        public void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            try
            {
                SynchronizationContext.SetSynchronizationContext(new
                   DispatcherSynchronizationContext(Application.Current.Dispatcher));
                SynchronizationContext.Current.Send(obj =>
                {
                    handler?.Invoke(this, new PropertyChangedEventArgs(name));
                }, null);
            }
            catch (Exception)
            {
                handler?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
    
}
