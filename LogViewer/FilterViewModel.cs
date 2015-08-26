using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GalaSoft.MvvmLight.CommandWpf;

namespace LogViewer
{
    public class FilterViewModel
    {
		private ICommand _applyFilterCommand;
	    private ICommand _clearFiltersCommand;
	    private ICommand _addFilterCommand;
	    private ICommand _removeFilterCommand;
	    private List<string> _columnNames;
		
		public FilterViewModel(List<string> columnNames)
	    {
			FilterObjects = new ObservableCollection<FilterObject>();
			ColumnNames = new ObservableCollection<string>(columnNames);
		    _columnNames = columnNames;
	    }
		
		/// <summary>
		/// Bound to the filter text input on the filter form
		/// </summary>
		public string FilterTextAreaValue { get; set; }

		/// <summary>
		/// Bound to the selectedValue of column names combo box
		/// </summary>
		public string SelectedColumn { get; set; }

		/// <summary>
		/// Bound to the column names combobox in the filter window
		/// </summary>
		public ObservableCollection<string> ColumnNames { get; set; }

		/// <summary>
		/// Bound to list box control for filters added
		/// </summary>
		public ObservableCollection<FilterObject> FilterObjects { get; set; }

	    public ICommand AddFilterCommand
	    {
		    get
		    {
			    if (_addFilterCommand == null)
			    {
				    _addFilterCommand = new RelayCommand(
					    () =>
					    {
							if (String.IsNullOrEmpty(SelectedColumn) || String.IsNullOrWhiteSpace(FilterTextAreaValue))
								return;

						    FilterObject filterObject = new FilterObject { ColumnName = SelectedColumn, FilterCriteria = FilterTextAreaValue };
						    FilterObjects.Add(filterObject);
						    ColumnNames.Remove(SelectedColumn);
					    },
						() => ColumnNames.Count > 0
					    );
			    }
			    return _addFilterCommand;
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

						},
						() => FilterObjects.Count > 0
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
						() =>
						{
							FilterObjects.Clear();
							foreach (var columnName in _columnNames.Where(columnName => !ColumnNames.Contains(columnName)))
							{
								ColumnNames.Add(columnName);
							}
						},
						() => FilterObjects.Count > 0
						);
				}
				return _clearFiltersCommand;
			}
		}

		public ICommand RemoveFilterObjectCommand(string columnName)
	    {
		    if (_removeFilterCommand == null)
		    {
				_removeFilterCommand = new RelayCommand(
				    () =>
				    {
					    if (String.IsNullOrEmpty(columnName))
						    return;

					    var filterObject = FilterObjects.FirstOrDefault(x => x.ColumnName.Equals(columnName));
					    FilterObjects.Remove(filterObject);
					    ColumnNames.Add(columnName);
				    },
				    () => true
				    );
		    }
			return _removeFilterCommand;
	    }
    }
}
