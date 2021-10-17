using AcerHumanPresence.command;
using ContextSensingClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AcerHumanPresence.viewModel
{
    class UserPresenceViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private ClientBackendLink _commClient = null;

        public ICommand EnableUserPresenceCommand { get; set; }

        private Boolean _isUserPresenceEnabled = true;
        public Boolean IsUserPresenceEnabled
        {
            get { return _isUserPresenceEnabled;  }
            set
            {
                _isUserPresenceEnabled = value;
                OnPropertyChange("IsUserPresenceEnabled");
            }
        }

        public UserPresenceViewModel(ClientBackendLink connector)
        {
            this._commClient = connector;
            EnableUserPresenceCommand = new RelayCommand(executeEnableUserPresence, canExecuteEnableUserPresence);
        }

        private void OnPropertyChange(string propertyname)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
            }
        }
        private bool canExecuteEnableUserPresence(object parameter)
        {
            return true;
        }

        private void executeEnableUserPresence(object parameter)
        {
            Boolean isChecked = (Boolean)parameter;

            try
            {
                if (isChecked)
                {
                    //await _commClient.SetOption(FeatureType.LOCK, FeatureProperty.FeatureEnabled, true, globalMode);
                    _commClient.Enable(FeatureType.LOCK);
                    //FeatureSetting walFeature = await _commClient.GetOptions(FeatureType.LOCK, globalMode);
                    _commClient.Enable(FeatureType.WAKE);
                    //FeatureSetting woaFeature = await _commClient.GetOptions(FeatureType.WAKE, globalMode);
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}
