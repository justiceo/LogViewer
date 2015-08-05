using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using GalaSoft.MvvmLight.CommandWpf;
using Newtonsoft.Json.Linq;

namespace LogViewer
{
	public class MainViewModel : INotifyPropertyChanged
	{
		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		#region Private Fields

		private IJEnumerable<JObject> _jObjectCollection;

		private ICommand _firstCommand;

		private ICommand _previousCommand;

		private ICommand _nextCommand;

		private ICommand _openFileCommand;

		private ICommand _lastCommand;

        private DataStore _dataStore;

		#endregion

		public MainViewModel()
		{
			InitDataStore();
		}

        public bool IsLargeFile()
        {
            return _dataStore.IsLargeFile;
        }

        public int StartRowIndex()
        {
            return _dataStore.CurrentStartIndex;
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

		/// <summary>
		/// Gets the information for text block in the navigation control
		/// </summary>
		public string GetNavigationInfo
		{
			get
			{
				if (_dataStore.IsHydrated)
					return _dataStore.PageNavigationString();

				return "No Pagination Data";
			}
		}

		/// <summary>
		/// Gets the command for moving to the first page of products.
		/// </summary>
		public ICommand FirstCommand
		{
			get
			{
				if (_firstCommand == null)
				{
					_firstCommand = new RelayCommand
						(
						() =>
						{
							JObjectCollection = _dataStore.FirstPage();
							NotifyAll();
						},
                        () => _dataStore.HasPreviousPage()
						);
				}

				return _firstCommand;
			}
		}

		/// <summary>
		/// Gets the command for moving to the previous page of products.
		/// </summary>
		public ICommand PreviousCommand
		{
			get
			{
				if (_previousCommand == null)
				{
					_previousCommand = new RelayCommand
						(
						() =>
						{
							JObjectCollection = _dataStore.PreviousPage();
							NotifyAll();
						},
                        () => _dataStore.HasPreviousPage()
						);
				}

				return _previousCommand;
			}
		}

		/// <summary>
		/// Gets the command for moving to the next page of products.
		/// </summary>
		public ICommand NextCommand
		{
			get
			{
				if (_nextCommand == null)
				{
					_nextCommand = new RelayCommand
						(
						() =>
						{
							JObjectCollection = _dataStore.NextPage();
							NotifyAll();
						},
                        () => _dataStore.HasNextPage()
						);
				}

				return _nextCommand;
			}
		}

		/// <summary>
		/// Gets the command for moving to the last page of products.
		/// </summary>
		public ICommand LastCommand
		{
			get
			{
				if (_lastCommand == null)
				{
					_lastCommand = new RelayCommand
						(
						() =>
						{
							JObjectCollection = _dataStore.LastPage();
							NotifyAll();
						},
                        () => _dataStore.HasLastPage()
						);
				}

				return _lastCommand;
			}
		}

		/// <summary>
		/// Gets the command for opening a log file
		/// </summary>
		public ICommand OpenFileCommand
		{
			get
			{
				if (_openFileCommand == null)
				{
					_openFileCommand = new RelayCommand
						(
						InitDataStore
						);
				}

				return _openFileCommand;
			}
		}

		/// <summary>
		/// The number of items to display in a page
		/// </summary>
		public int PageSize
		{
			get { return _dataStore.UserDefinedPageSize; }
			set
			{
                _dataStore.UserDefinedPageSize = value;
				JObjectCollection = _dataStore.ResizeCurrentPage();
				NotifyAll();
			}
		}

		public int[] PageSizeOptions
		{
			get
			{
				if (_dataStore.IsHydrated)
				{
					List<int> possibleOptions = new List<int> {50, 100, 200, 500, 1000, 5000, 10000};
					possibleOptions = possibleOptions.Where(s => s < _dataStore.TotalRecordCount).ToList();
					return possibleOptions.ToArray();
				}

				return new[] {0};
			}
		}

		/// <summary>
		/// Sorts the list of products.
		/// </summary>
		/// <param name="sortColumn">The column or member that is the basis for sorting.</param>
		/// <param name="ascending">Set to true if the sort</param>
		public void Sort(string sortColumn)
		{
			JObjectCollection = _dataStore.ApplySort(sortColumn);
			NotifyAll();
		}

		/// <summary>
		/// Refreshes the list of log entries. Called by navigation commands.
		/// </summary>
		private void InitDataStore()
		{
            _dataStore = new DataStore();
            _dataStore.LoadFile();
			JObjectCollection = _dataStore.FirstPage();
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

		private void NotifyAll()
		{
			NotifyPropertyChanged("GetNavigationInfo");
		}
	}
}