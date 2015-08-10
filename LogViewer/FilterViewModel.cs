using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight.CommandWpf;

namespace LogViewer
{
    public class FilterViewModel
    {
		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		private List<FilterObject> _filterObjects;
	    private ICommand _addFilterObjectCommand;
	    private ICommand _applyFilterCommand;
	    private ICommand _clearFiltersCommand;
	    private readonly List<string> _columnNames; 

	    public FilterViewModel(List<string> columnNames )
	    {
			_filterObjects = new List<FilterObject>();
		    _columnNames = columnNames;
	    }


		public string ColumnsComboBoxSelectedValue { get; set; }

		/// <summary>
		/// Bound to the filter text input on the filter form
		/// </summary>
		public string FilterTextAreaValue { get; set; }

		/// <summary>
		/// Bound to the selectedValue of column names combo box
		/// </summary>
		public string SelectedColumn { get; set; }
		
		public ICommand AddFilterObjectCommand
		{
			get
			{
				if (_addFilterObjectCommand == null)
				{
					_addFilterObjectCommand = new RelayCommand(
						AddFilterObject,
						CanAddFilterObject
						);
				}
				return _addFilterObjectCommand;
			}
		}

	    public ICommand ApplyFilterCommand
	    {
		    get
		    {
			    if (_applyFilterCommand == null)
			    {
				    _applyFilterCommand = new RelayCommand(
					    () =>
					    {
						    ApplyFilter();
					    },
						() => _filterObjects.Count > 0
					    );
			    }
			    return _applyFilterCommand;
		    }
	    }

		public ICommand ClearFiltersCommand
		{
			get
			{
				if (_clearFiltersCommand == null)
				{
					_clearFiltersCommand = new RelayCommand(
						ClearFilters,
						() => _filterObjects.Count > 0
						);
				}
				return _clearFiltersCommand;
			}
		}

		/// <summary>
		/// Bound to the column names combobox in the filter window
		/// </summary>
		public List<string> ColumnNames {
			get { return _columnNames; }
		}

	    public List<FilterObject> FilterObjectsList
	    {
		    get { return _filterObjects; }
			set { _filterObjects = value;  }
	    }

	    private void ApplyFilter()
	    {
		    
	    }

		private void ClearFilters()
		{
			_filterObjects.Clear();
		}

		private void AddFilterObject()
		{
			if (String.IsNullOrWhiteSpace(SelectedColumn) || String.IsNullOrWhiteSpace(FilterTextAreaValue))
				return;

			FilterObject filterObject = new FilterObject
			{
				ColumnName = SelectedColumn,
				FilterCriteria = FilterTextAreaValue
			};
			_filterObjects.Add(filterObject);

			SelectedColumn = "";
			FilterTextAreaValue = "";
			NotifyAll();
		}

	    private bool CanAddFilterObject()
	    {
		    if (_columnNames.Count == 0 || String.IsNullOrEmpty(SelectedColumn))
			    return false;

		    return true;
	    }

		private void NotifyPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		private void NotifyAll()
		{
			NotifyPropertyChanged("ApplyFilterCommand");
			NotifyPropertyChanged("ClearFiltersCommand");
			NotifyPropertyChanged("AddFilterObjectCommand");
			NotifyPropertyChanged("FilterObjectsList");
			NotifyPropertyChanged("FilterTextAreaValue");
			NotifyPropertyChanged("SelectedColumn");
		}
    }
}
