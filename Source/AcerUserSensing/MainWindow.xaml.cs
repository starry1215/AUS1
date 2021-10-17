using AcerUserSensing.Properties;
using ContextSensingClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace AcerUserSensing
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
	{
		private readonly int[] preDimIntervalSeconds = new int[] { 0, 5, 10, 20, 30, 60 };
		private readonly int[] lockAfterDimIntervalsSeconds = new int[] { 5, 10, 20, 30, 60 };
		private readonly int INDEX_PRE_DIM = 1;
		private readonly int INDEX_LOCK_AFTER_DIM = 0;

		private ClientBackendLink _commClient = null;
		private FeatureCallback featureCallback;
		private SettingsAccessType globalMode = SettingsAccessType.LOCAL;
		private static Logger logger = new Logger();
		public MainWindow()
		{
			InitializeComponent();

			InitDefaultData();
			featureCallback = new FeatureCallback();
            featureCallback.onFeatureEvent += FeatureCallback_onEvent;
            featureCallback.onFeatureError += FeatureCallback_onError;
            featureCallback.onFeatureSuccess += FeatureCallback_onSuccess;
		}
		private async void AUS_Loaded(object sender, RoutedEventArgs e)
		{
			_commClient = new ClientBackendLink(featureCallback);

			Result ret = await RefreshFeaturesViaGetOptions();
			if (ret.Success)
			{
				SetApplicationSettings();
			}
			else
			{
				MessageBox.Show(ret.Reason, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void AUS_Unloaded(object sender, RoutedEventArgs e)
		{
			featureCallback.onFeatureEvent -= FeatureCallback_onEvent;
			featureCallback.onFeatureError -= FeatureCallback_onError;
			featureCallback.onFeatureSuccess -= FeatureCallback_onSuccess;

			cb_enableAll.Click -= Cb_enableAll_Click;
			cb_enableWAL.Click -= Cb_enableWAL_Click;
			cb_enableWOA.Click -= Cb_enableWOA_Click;
			cb_externalMonitor.Click -= Cb_externalMonitor_Click;
			//cb_charger.Click -= Cb_charger_Click;
		}

		private void ResetControls()
        {
			cb_enableWAL.IsChecked = false;
			cb_enableWOA.IsChecked = false;
			cb_enableAll.IsChecked = false;
        }

		private void EnableControls(Boolean enabled = true)
        {
			cb_enableAll.IsEnabled = enabled;
			cb_enableWAL.IsEnabled = enabled;
			cb_enableWOA.IsEnabled = enabled;
        }

		private void InitDefaultData()
		{
			//popup_globalConfig.CustomPopupPlacementCallback = new CustomPopupPlacementCallback(placePopup);

			string str_seconds = "Seconds";
			string str_immediate = "Immediately";
			try
            {
				ResourceManager rm = new ResourceManager("AcerUserSensing.Properties.Resources", Assembly.GetExecutingAssembly());
				str_seconds = rm.GetString("strSecond");
				str_immediate = rm.GetString("strImmediate");
			}
			catch
            {

            }

			combobox_dimDelay.ItemsSource = Array.ConvertAll(preDimIntervalSeconds, x => x > 0 ? x.ToString() + " " + str_seconds : str_immediate).ToList();
			combobox_lockDelay.ItemsSource = Array.ConvertAll(lockAfterDimIntervalsSeconds, x => x > 0 ? x.ToString() + " " + str_seconds : str_immediate).ToList();
			//keep the default setting on 5 seconds
			combobox_dimDelay.SelectedIndex = INDEX_PRE_DIM;
			combobox_lockDelay.SelectedIndex = INDEX_LOCK_AFTER_DIM;

            cb_enableAll.Click += Cb_enableAll_Click;
			cb_enableWAL.Click += Cb_enableWAL_Click;
			cb_enableWOA.Click += Cb_enableWOA_Click;
			cb_externalMonitor.Click += Cb_externalMonitor_Click;
			//cb_charger.Click += Cb_charger_Click;

			combobox_dimDelay.SelectionChanged += Combobox_dimDelay_SelectionChanged;
			combobox_lockDelay.SelectionChanged += Combobox_lockDelay_SelectionChanged;
		}

        private async void Cb_enableAll_Click(object sender, RoutedEventArgs e)
        {
			//turn on/off both WAL and WOA
            CheckBox c = sender as CheckBox;

			if (c.IsChecked == true)
			{
				try
				{
					cb_enableWOA.IsEnabled = cb_enableWAL.IsEnabled = false;

					if (globalMode == SettingsAccessType.GLOBAL)
					{
						await _commClient.SetOption(FeatureType.LOCK, FeatureProperty.FeatureEnabled, true, globalMode);
						await _commClient.SetOption(FeatureType.WAKE, FeatureProperty.FeatureEnabled, true, globalMode);
					}
					else
					{
						_commClient.Enable(FeatureType.LOCK);
						_commClient.Enable(FeatureType.WAKE);
					}

					FeatureSetting walFeature = await _commClient.GetOptions(FeatureType.LOCK, globalMode);
					UpdateTimerControlSettings(walFeature);

					cb_enableWAL.IsChecked = cb_enableWOA.IsChecked = true;
					cb_externalMonitor.IsEnabled = true;
				}
				catch (Exception ex)
				{
					cb_enableAll.IsChecked = false;
					logger.Error(ex.Message);
					//MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					cb_enableWOA.IsEnabled = cb_enableWAL.IsEnabled = true;
				}
			}
			else
			{
				try
				{
					cb_enableWOA.IsEnabled = cb_enableWAL.IsEnabled = false;

					if (globalMode == SettingsAccessType.GLOBAL)
					{
						await _commClient.SetOption(FeatureType.LOCK, FeatureProperty.FeatureEnabled, false, globalMode);
						await _commClient.SetOption(FeatureType.WAKE, FeatureProperty.FeatureEnabled, false, globalMode);
						FeatureSetting walFeature = await _commClient.GetOptions(FeatureType.LOCK, globalMode);
						UpdateTimerControlSettings(walFeature);
					}
					else
					{
						_commClient.Disable(FeatureType.LOCK);
						_commClient.Disable(FeatureType.WAKE);
					}

					cb_enableWAL.IsChecked = cb_enableWOA.IsChecked = false;
					cb_externalMonitor.IsEnabled = false;
				}
				catch (Exception ex)
				{
					cb_enableAll.IsChecked = true;
					logger.Error(ex.Message);
					//MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					cb_enableWOA.IsEnabled = cb_enableWAL.IsEnabled = true;
				}
			}
		}

        private async void Cb_enableWAL_Click(object sender, RoutedEventArgs e)
		{
			//turn on/off WAL
			CheckBox c = sender as CheckBox;

			try
			{
				c.IsEnabled = false;

				if (c.IsChecked == true)
				{
					if (globalMode == SettingsAccessType.GLOBAL)
					{
						await _commClient.SetOption(FeatureType.LOCK, FeatureProperty.FeatureEnabled, true, globalMode);
					}
					else
                    {
						_commClient.Enable(FeatureType.LOCK);
					}
						
					FeatureSetting walFeature = await _commClient.GetOptions(FeatureType.LOCK, globalMode);
					UpdateTimerControlSettings(walFeature);
				}
				else
				{
					if (globalMode == SettingsAccessType.GLOBAL)
					{
						await _commClient.SetOption(FeatureType.LOCK, FeatureProperty.FeatureEnabled, false, globalMode);
						FeatureSetting walFeature = await _commClient.GetOptions(FeatureType.LOCK, globalMode);
						UpdateTimerControlSettings(walFeature);
					}
					else
                    {
						_commClient.Disable(FeatureType.LOCK);
					}
				}

				UpdateGlobalCheckbox();
			}
			catch (Exception ex)
			{
				c.IsChecked = !c.IsChecked;
				logger.Error(ex.Message);
				//MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				c.IsEnabled = true;
			}
		}
		private async void Cb_enableWOA_Click(object sender, RoutedEventArgs e)
		{
			//turn on/off WOA
			CheckBox c = sender as CheckBox;

			try
			{
				c.IsEnabled = false;

				if (c.IsChecked == true)
				{
					if (globalMode == SettingsAccessType.GLOBAL)
					{
						await _commClient.SetOption(FeatureType.WAKE, FeatureProperty.FeatureEnabled, true, globalMode);
					}
					else
                    {
						_commClient.Enable(FeatureType.WAKE);
					}
				}
				else
				{
					if (globalMode == SettingsAccessType.GLOBAL)
					{
						await _commClient.SetOption(FeatureType.WAKE, FeatureProperty.FeatureEnabled, false, globalMode);
					}
					else
                    {
						_commClient.Disable(FeatureType.WAKE);
					}
				}

				UpdateGlobalCheckbox();
			}
			catch (Exception ex)
			{
				c.IsChecked = !c.IsChecked;
				logger.Error(ex.Message);
				//MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				c.IsEnabled = true;
			}
		}

		private async void Cb_externalMonitor_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				CheckBox c = sender as CheckBox;
				await _commClient.SetOption(FeatureType.LOCK, FeatureProperty.EnableWithExtMonitor, c.IsChecked, globalMode);
				await _commClient.SetOption(FeatureType.WAKE, FeatureProperty.EnableWithExtMonitor, c.IsChecked, globalMode);
			}
			catch (Exception ex)
			{
				cb_externalMonitor.IsChecked = !cb_externalMonitor.IsChecked;
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		/*
		private async void Cb_charger_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				CheckBox c = sender as CheckBox;
				if (c.IsChecked == true)
				{
					//disable on battery mode
					await _commClient.SetOption(FeatureType.WAKE, FeatureProperty.WakeOnLowBattery, false, globalMode);
					await _commClient.SetOption(FeatureType.WAKE, FeatureProperty.LowBatteryRemainingPercentageLimit, 100, globalMode);
					//support on lock?
					await _commClient.SetOption(FeatureType.LOCK, FeatureProperty.WakeOnLowBattery, false, globalMode);
					await _commClient.SetOption(FeatureType.LOCK, FeatureProperty.LowBatteryRemainingPercentageLimit, 100, globalMode);
				}
				else
				{
					await _commClient.SetOption(FeatureType.WAKE, FeatureProperty.WakeOnLowBattery, true, globalMode);
					//support on lock?
					await _commClient.SetOption(FeatureType.LOCK, FeatureProperty.WakeOnLowBattery, true, globalMode);
				}
			}
			catch (Exception ex)
			{
				cb_charger.IsChecked = !cb_charger.IsChecked;
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		*/

		public CustomPopupPlacement[] placePopup(Size popupSize, Size targetSize, Point offset)
		{
			CustomPopupPlacement placement1 =
			   new CustomPopupPlacement(new Point(-362, 50), PopupPrimaryAxis.Horizontal);

			CustomPopupPlacement placement2 =
				new CustomPopupPlacement(new Point(10, 20), PopupPrimaryAxis.Vertical);

			CustomPopupPlacement[] ttplaces =
					new CustomPopupPlacement[] { placement1, placement2 };
			return ttplaces;
		}

		private void Combobox_lockDelay_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (sender is ComboBox)
			{
				if (((ComboBox)sender).SelectedIndex < lockAfterDimIntervalsSeconds.Length)
				{
					Task setTask = _commClient.SetOption(FeatureType.LOCK, FeatureProperty.LockAfterDimInterval, lockAfterDimIntervalsSeconds[((ComboBox)sender).SelectedIndex] * 1000, globalMode);
					HandleSetOptionExceptions(setTask);
				}
			}
		}

		private void UpdateGlobalCheckbox()
        {
			cb_enableAll.IsChecked = cb_enableWAL.IsChecked == true | cb_enableWOA.IsChecked == true;
			cb_externalMonitor.IsEnabled = cb_enableWAL.IsChecked == true & cb_enableWOA.IsChecked == true;
		}

		private void Combobox_dimDelay_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (sender is ComboBox)
			{
				if (((ComboBox)sender).SelectedIndex < preDimIntervalSeconds.Length)
				{
					Task setTask = _commClient.SetOption(FeatureType.LOCK, FeatureProperty.PreDimInterval, preDimIntervalSeconds[((ComboBox)sender).SelectedIndex] * 1000, globalMode);
					HandleSetOptionExceptions(setTask);
				}
			}
		}

		internal async Task<Result> RefreshFeaturesViaGetOptions()
		{
			if (_commClient == null)
			{
				_commClient = new ClientBackendLink(featureCallback);
			}

			Result ret = await UpdateControlsViaGetOptions(FeatureType.LOCK);
			if (ret.Success)
			{
				ret = await UpdateControlsViaGetOptions(FeatureType.WAKE);
			}
			if (ret.Success)
			{
				this.UpdateGlobalCheckbox();
			}

			return ret;
		}

		private async Task<Result> UpdateControlsViaGetOptions(FeatureType featureType)
		{
			Result ret = new Result();
			try
			{
				FeatureSetting featureSetting = await _commClient.GetOptions(featureType, globalMode);
				UpdateControlSettings(featureSetting);
			}
			catch (ErrorException ex)
			{
				logger.Error(ex.errorObject.Description);
				ret.Reason = String.IsNullOrEmpty(ex.errorObject.Description) ? "Unknown" : ex.Message;
			}

			return ret;
		}

		internal void UpdateControlSettings(FeatureSetting featureSetting)
		{
			if (featureSetting is LockFeatures)
			{
				UpdateWALControlSettings((LockFeatures)featureSetting);
			}
			else if (featureSetting is WakeFeatures)
			{
				UpdateWOAControlSettings((WakeFeatures)featureSetting);
			}
		}
		private void UpdateWALControlSettings(LockFeatures lockFeature)
		{
			cb_enableWAL.IsChecked = lockFeature.WALEnabled;
			
			int preDimIndex = Array.IndexOf(preDimIntervalSeconds, lockFeature.PreDimInterval / 1000);
			combobox_dimDelay.SelectedIndex = preDimIndex >= 0 ? preDimIndex : INDEX_PRE_DIM;
			int lockAfterIndex = Array.IndexOf(lockAfterDimIntervalsSeconds, lockFeature.LockAfterDimInterval / 1000);
			combobox_lockDelay.SelectedIndex = lockAfterIndex >= 0 ? lockAfterIndex : INDEX_LOCK_AFTER_DIM;	 

			//Update external monitor depends on WAL
			cb_externalMonitor.IsChecked = lockFeature.EnableLockWithExternalMonitor;
		}

		private void UpdateWOAControlSettings(WakeFeatures wakeFeature)
		{
			cb_enableWOA.IsChecked = wakeFeature.WOAEnabled;

			//Update external monitor depends on WOA. It should be the same with lockFeature.EnableLockWithExternalMonitor.
			cb_externalMonitor.IsChecked = wakeFeature.EnableWakeWithExternalMonitor;
			//Update charger depends on WOA only since no relative feature on WAL.
			//cb_charger.IsChecked = !wakeFeature.WakeOnLowBattery;
		}

		private void UpdateTimerControlSettings(FeatureSetting featureSetting)
		{
			if (featureSetting is LockFeatures)
			{
				combobox_dimDelay.SelectionChanged -= Combobox_dimDelay_SelectionChanged;
				int preDimIndex = Array.IndexOf(preDimIntervalSeconds, ((LockFeatures)featureSetting).PreDimInterval / 1000);
				combobox_dimDelay.SelectedIndex = preDimIndex >= 0 ? preDimIndex : INDEX_PRE_DIM;
				combobox_dimDelay.SelectionChanged += Combobox_dimDelay_SelectionChanged;

				combobox_lockDelay.SelectionChanged -= Combobox_lockDelay_SelectionChanged;
				int lockAfterIndex = Array.IndexOf(lockAfterDimIntervalsSeconds, ((LockFeatures)featureSetting).LockAfterDimInterval / 1000);
				combobox_lockDelay.SelectedIndex = lockAfterIndex >= 0 ? lockAfterIndex : INDEX_LOCK_AFTER_DIM;
				combobox_lockDelay.SelectionChanged += Combobox_lockDelay_SelectionChanged;
			}
		}

		private async void SetApplicationSettings()
		{
			bool launchOnUserLogIn = false;
			string appName = "AcerUserSensing";
			string protocol = "acerusersensing";
			List<string> enabledEvents = Settings.Default.EnabledEvents.Cast<string>().ToList();

			Task setTask = _commClient.SetOption(FeatureType.APPLICATION, FeatureProperty.EnabledEvents, enabledEvents);
			HandleSetOptionExceptions(setTask);
			setTask = _commClient.SetOption(FeatureType.APPLICATION, FeatureProperty.AppName, appName);
			HandleSetOptionExceptions(setTask);
			setTask = _commClient.SetOption(FeatureType.APPLICATION, FeatureProperty.Protocol, protocol);
			HandleSetOptionExceptions(setTask);
			setTask = _commClient.SetOption(FeatureType.APPLICATION, FeatureProperty.LaunchOnUserLogIn, launchOnUserLogIn);
			HandleSetOptionExceptions(setTask);
		}

		private async void HandleSetOptionExceptions(Task setTask)
		{
			try
			{
				await setTask;
			}
			catch (ErrorException ee)
			{
				logger.Debug(ee.errorObject.Description);
				MessageBox.Show(ee.errorObject.Description, ee.errorType.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
			}
			catch (AggregateException ae)
			{
				foreach (var ie in ae.InnerExceptions)
				{
					ErrorException ee = (ErrorException)ie;
					logger.Debug(ee.errorObject.Description);
					MessageBox.Show(ee.errorObject.Description, ee.errorType.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			catch (Exception ex)
			{
				logger.Debug(ex.Message);
			}
		}

        private async void link_windowsHello_Click(object sender, RoutedEventArgs e)
        {
			await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:signinoptions"));
		}

		private void FeatureCallback_onSuccess(object sender, FeatureType featureType, ResponseType responseType)
		{
			try
			{
				logger.Info("Feature: " + featureType.ToString() + " on success with response " + responseType.ToString() + " from service");

				switch (featureType)
				{
					case FeatureType.LOCK:
						{
							cb_enableWAL.IsEnabled = true;
						}
						break;
					case FeatureType.WAKE:
						{
							cb_enableWOA.IsEnabled = true;
						}
						break;
					case FeatureType.APPLICATION:
						{
							logger.Info("Enable/Disable success for Privacy " + responseType.ToString());
						}
						break;
				}

			}
			catch (Exception e)
			{
				logger.Debug(e.Message);
				logger.Error("Exception on handling OnSuccess event from service");
			}
		}

		private void FeatureCallback_onError(object sender, FeatureType featureType, Error state)
		{
			try
			{
				logger.Error("Feature: " + featureType.ToString() + " on error " + state.Description + " from service");

				switch (state.ErrorType)
				{
					case ErrorType.ServiceStopped:
					case ErrorType.ServiceUnavailable:
						{
							Application.Current.Dispatcher.BeginInvoke(
								new Action(() => {
									this.ResetControls();
									this.EnableControls(true);
									MessageBox.Show(state.Description, state.ErrorType.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
								}
							), System.Windows.Threading.DispatcherPriority.Normal);
						}
						break;
					case ErrorType.ServiceRecoveryInProgress:
						{
							Application.Current.Dispatcher.BeginInvoke(
								new Action(() => {
									this.ResetControls();
									this.EnableControls(false);

									logger.Debug(state.Description);
									MessageBox.Show(state.Description, state.ErrorType.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
								}
							), System.Windows.Threading.DispatcherPriority.Normal);

						}
						break;
					case ErrorType.SetOptionError:
						{
							MessageBox.Show(state.Description, "Setting Option for " + featureType + "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
							logger.Error(state.Description);
						}
						break;
					default:
						{
							switch (featureType)
							{
								case FeatureType.LOCK:
									{
										Application.Current.Dispatcher.BeginInvoke(
											new Action(() => {
												if (cb_enableWAL.IsChecked == true)
												{
													cb_enableWAL.IsChecked = false;
													logger.Error(state.Description);
												}
												MessageBox.Show(state.Description, state.ErrorType.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
											}
										), System.Windows.Threading.DispatcherPriority.Normal);
									}
									break;
								case FeatureType.WAKE:
									{
										Application.Current.Dispatcher.BeginInvoke(
											new Action(() => {
												if (cb_enableWOA.IsChecked == true)
												{
													cb_enableWOA.IsChecked = false;

													logger.Error(state.Description);
												}
												MessageBox.Show(state.Description, state.ErrorType.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
											}
										), System.Windows.Threading.DispatcherPriority.Normal);
									}
									break;
							}
						}
						break;
				}
			}
			catch (Exception e)
			{
				logger.Debug(e.Message);
				logger.Error("Exception on handling OnError event from service");
			}
		}

		private void FeatureCallback_onEvent(object sender, FeatureType featureType, EventType eventType, object eventPayload)
		{
			try
			{
				logger.Info("Event " + eventType + " for feature " + featureType + " received from service. Event payload " + eventPayload?.ToString());

				switch (eventType)
				{
					case EventType.EVENT_EXTERNAL_MONITOR:
						{
							logger.Debug("External Monitor Connected");
						}
						break;
					case EventType.EVENT_WAL:
						{
							logger.Debug("WAL event");
						}
						break;
					case EventType.EVENT_WOA:
						{
							logger.Debug("WOA event");
						}
						break;
					case EventType.EVENT_SERVICE_RECOVERED:
						{
							Application.Current.Dispatcher.BeginInvoke(
								new Action(() =>
								{
									this.EnableControls(true);
									this.RefreshFeaturesViaGetOptions();
								}
							), System.Windows.Threading.DispatcherPriority.Normal);
						}
						break;
					case EventType.EVENT_DTT_STARTED:
						{
							new Thread(() =>
							{
								MessageBox.Show("Detected DTT process start", "Process Event", MessageBoxButton.OK);
							}).Start();
						}
						break;
				}
			}
			catch (Exception e)
			{
				logger.Debug(e.Message);
				logger.Error("Exception on handling OnEvent event from service");
			}
		}

		private static class NativeMethods
		{
#if (DEBUG)
            [DllImport("kernel32.dll")]
            public static extern Boolean AllocConsole();
            [DllImport("kernel32.dll")]
            public static extern Boolean FreeConsole();
#endif
			[DllImport("PowrProf.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
			public static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);
		}
		internal enum WindowShowStyle : uint
		{
			Hide = 0,
			ShowNormal = 1,
			ShowMinimized = 2,
			ShowMaximized = 3,
			Maximize = 3,
			ShowNormalNoActivate = 4,
			Show = 5,
			Minimize = 6,
			ShowMinNoActivate = 7,
			ShowNoActivate = 8,
			Restore = 9,
			ShowDefault = 10,
			ForceMinimized = 11
		}
	}
}
