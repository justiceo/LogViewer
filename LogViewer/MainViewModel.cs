using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace LogViewer
{
	public class MainViewModel : INotifyPropertyChanged
	{
		
		#region Private Fields

		private IJEnumerable<JObject> _jObjectCollection;
		private ICommand _firstCommand;
		private ICommand _previousCommand;
		private ICommand _nextCommand;
        private ICommand _openFileCommand;
        private ICommand _openFileAsStreamCommand;
		private ICommand _lastCommand;
        private ICommand _reverseOrderCommand;
        private DataStore _dataStore;

	    #endregion

		public MainViewModel()
		{
            JObjectCollection = new JEnumerable<JObject>();
            _dataStore = new DataStore(" ", 0);
		}

		public event PropertyChangedEventHandler PropertyChanged;

        public bool IsLargeFile()
        {
            return _dataStore.IsLargeFile;
        }
		
		/// <summary>
		/// The list of logEntries in the current page.
		/// </summary>
		public IJEnumerable<JObject> JObjectCollection
		{
			get { return _jObjectCollection; }
			private set
			{
				if (ReferenceEquals(_jObjectCollection, value) != true)
				{
					_jObjectCollection = value;
					NotifyPropertyChanged("JObjectCollection");
				}
			}
		}

        #region Page Navigation

		public int StartRowIndex()
		{
			return _dataStore.GetCurrentStartIndex();
		}

		/// <summary>
		/// Gets the information for text block in the navigation control
		/// </summary>
		public string GetNavigationInfo
		{
			get
			{
				return _dataStore.GetPageNavigationString();
			}
		}

		/// <summary>
		/// Gets the command for moving to the first page of products.
		/// </summary>
		public ICommand FirstCommand
		{
			get
			{
				return _firstCommand ?? (_firstCommand = new RelayCommand
					(
					() =>
					{
						JObjectCollection = _dataStore.GetPage(Page.First);
						NotifyPropertyChanged("GetNavigationInfo");
					},
					() => _dataStore.HasPage(Page.Previous)
					));
			}
		}

		/// <summary>
		/// Gets the command for moving to the previous page of products.
		/// </summary>
		public ICommand PreviousCommand
		{
			get
			{
				return _previousCommand ?? (_previousCommand = new RelayCommand
					(
					() =>
					{
						JObjectCollection = _dataStore.GetPage(Page.Previous);
						NotifyPropertyChanged("GetNavigationInfo");
					},
					() => _dataStore.HasPage(Page.Previous)
					));
			}
		}

		/// <summary>
		/// Gets the command for moving to the next page of products.
		/// </summary>
		public ICommand NextCommand
		{
			get
			{
				return _nextCommand ?? (_nextCommand = new RelayCommand
					(
					() =>
					{
						JObjectCollection = _dataStore.GetPage(Page.Next);
						NotifyPropertyChanged("GetNavigationInfo");
					},
					() => _dataStore.HasPage(Page.Next)
					));
			}
		}

		/// <summary>
		/// Gets the command for moving to the last page of products.
		/// </summary>
		public ICommand LastCommand
		{
			get
			{
				return _lastCommand ?? (_lastCommand = new RelayCommand
					(
					() =>
					{
						JObjectCollection = _dataStore.GetPage(Page.Last);
						NotifyPropertyChanged("GetNavigationInfo");
					},
					() => _dataStore.HasPage(Page.Last)
					));
			}
		}

        #endregion

        /// <summary>
		/// Gets the command for opening a log file
		/// </summary>
		public ICommand OpenFileCommand
		{
			get
			{
				return _openFileCommand ?? (_openFileCommand = new RelayCommand
					(
					() => InitDataStore(isFileStream: false)
					));
			}
		}

		public ICommand OpenAsStreamCommand
		{
			get
			{
				return _openFileAsStreamCommand ?? (_openFileAsStreamCommand = new RelayCommand
					(
					() => InitDataStore(isFileStream: true)
					));
			}
		}

        public ICommand ReverseOrderCommand
        {
            get
            {
                if (_reverseOrderCommand == null)
                {
                    _reverseOrderCommand = new RelayCommand
                        (
                        () => JObjectCollection = _dataStore.ReverseOrder()
                        );
                }

                return _reverseOrderCommand;
            }
        }

        #region Page Sizing

		/// <summary>
		/// The number of items to display in a page
		/// </summary>
		public int PageSize
		{
			get { return _dataStore.UserDefinedPageSize; }
			set
			{
                _dataStore.UserDefinedPageSize = value;
                JObjectCollection = _dataStore.ResizePage(); 
                NotifyPropertyChanged("GetNavigationInfo");
			}
		}

	    public bool PageSizingEnabled
	    {
            get { return _dataStore.GetTotalRecordCount() > 50; }
	    }

		public int[] PageSizeOptions
		{
			get
			{
				// consider processing and returning this value from dataStore
				if(_dataStore.GetTotalRecordCount() == 0)
					return new[] {1};

				List<int> possibleOptions = new List<int> {50, 100, 200, 500, 1000, 5000, 10000};
				possibleOptions = possibleOptions.Where(s => s < _dataStore.GetTotalRecordCount()).ToList();
				return possibleOptions.ToArray();
			}
		}

        #endregion

        #region Document Section

	    public bool DocSectionEnabled
	    {
	        get { return _dataStore.IsLargeFile; } 
	    }

		public int[] DocSections
		{
			get { return new[] {1,2}; }
		}

		public int SelectedDocSection
		{
			get { return _dataStore.GetTotalRecordCount()/_dataStore.DocSectionSize; }
			set { _dataStore.DefaultStartRowIndex = value; }
		}

		#endregion

        /// <summary>
		/// Sorts the list of products.
		/// </summary>
		/// <param name="sortColumn">The column or member that is the basis for sorting.</param>
		/// <param name="ascending">Set to true if the sort</param>
		public void Sort(string sortColumn)
		{
			JObjectCollection = _dataStore.SortBy(sortColumn);
			NotifyAll();
		}

		/// <summary>
		/// Refreshes the list of log entries. Called by navigation commands.
		/// </summary>
		private void InitDataStore(bool isFileStream)
		{
			// Create an instance of the open file dialog box.
			OpenFileDialog openFileDialog = new OpenFileDialog();

			// Call the ShowDialog method to show the dialog box. Process input if the user clicked OK.
			if (openFileDialog.ShowDialog() == false)
			{
				_dataStore = _dataStore ?? new DataStore(null, 0);
				return;
			}
			if (string.IsNullOrWhiteSpace(openFileDialog.FileName))
			{
				_dataStore = _dataStore ?? new DataStore(null, 0);
				return;
			}
			// check if file exists

            _dataStore = isFileStream 
                ? new CacheDataStore(openFileDialog.FileName, 0)  
                : new DataStore(openFileDialog.FileName, 0);

			_dataStore.LoadFile();
			JObjectCollection = _dataStore.GetPage(Page.First);
			NotifyAll();
		}

		/// <summary>
		/// Notifies subscribers of changed properties.
		/// </summary>
		/// <param name="propertyName">Name of the changed property.</param>
		private void NotifyPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		/// <summary>
		/// Fires property changed on this view models UI controls
		/// </summary>
		private void NotifyAll()
		{
			NotifyPropertyChanged("GetNavigationInfo");
            NotifyPropertyChanged("PageSizingEnabled");
            NotifyPropertyChanged("PageSizeOptions");
            NotifyPropertyChanged("PageSize");
            NotifyPropertyChanged("DocSectionEnabled");
		}
		
		/// <summary>
		/// Calls filter on the datastore
		/// ***This functionality is not part of the viewmodel and needs to be refactored out.
		/// </summary>
		/// <param name="filterCriteria"></param>
		public void Filter(List<FilterObject> filterCriteria)
		{
			JObjectCollection = _dataStore.Filter(filterCriteria);
			NotifyAll();
		}
	}
}