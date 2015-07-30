using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

		private int _start = 0;

		private const int ItemCount = 50;

		private string _sortColumn = "Id";

		private bool _ascending = true;

		private int _totalItems = 0;

		private ICommand _firstCommand;

		private ICommand _previousCommand;

		private ICommand _nextCommand;

		private ICommand _lastCommand;

		#endregion

		public MainViewModel()
		{
			LoadLogEntries();
		}

		/// <summary>
		/// The list of logEntries in the current page.
		/// </summary>
		public IJEnumerable<JObject> JObjectCollection
		{
			get
			{
				return _jObjectCollection;
			}
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
				if(DataStore.IsHydrated)
					return DataStore.PageNavigationString();
				
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
							JObjectCollection = DataStore.FirstPage();
							NotifyAll();
						},
						DataStore.HasPreviousPage
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
							JObjectCollection = DataStore.PreviousPage();
							NotifyAll();
						},
						DataStore.HasPreviousPage
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
							JObjectCollection = DataStore.NextPage();
							NotifyAll();
						},
						DataStore.HasNextPage
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
							JObjectCollection = DataStore.LastPage();
							NotifyAll();
						},
						DataStore.HasNextPage
					);
				}

				return _lastCommand;
			}
		}

		/// <summary>
		/// The number of items to display in a page
		/// </summary>
		public int PageSize {
			get { return DataStore.UserDefinedPageSize; }
			set
			{
				DataStore.UserDefinedPageSize = value;
				JObjectCollection = DataStore.RefreshPage();
				NotifyAll();
			}
		}

		public int[] PageSizeOptions
		{

			get
			{
				if (DataStore.IsHydrated)
				{
					List<int> possibleOptions = new List<int> { 50, 100, 200, 500, 1000, 5000, 10000 };
					possibleOptions = possibleOptions.Where(s => s < DataStore.TotalRecordCount).ToList();
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
			_sortColumn = sortColumn;

			JObjectCollection = DataStore.ApplySort(_sortColumn);
			NotifyAll();
		}
		
		/// <summary>
		/// Refreshes the list of log entries. Called by navigation commands.
		/// </summary>
		private void LoadLogEntries()
		{
			DataStore.LoadCollection();
			JObjectCollection = DataStore.FirstPage();
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
