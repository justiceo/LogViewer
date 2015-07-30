using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogViewer
{
	public static class DataStore
	{

		private static List<JObject> JObjectsEnumerable;
		private static int _startRowIndex = 0;
		private static int _maximumRows = 100;
		private static int _totalRecordCount = 0;
		private static string _previousSortColumn = "";
		private static bool _isAscending = true;
		private static int _defaultStartRowIndex = 0; // can be updated by section

		public static bool HasMultipleObjects = false;
		public static bool IsHydrated = false;
		public static int UserDefinedPageSize = 100;

		public static int TotalRecordCount
		{
			get { return _totalRecordCount; }
			set { _totalRecordCount = value; }
		}

		public static void LoadCollection()
		{
			//if (JObjectsEnumerable != null)
			//{
			//	IsHydrated = true;
			//	return;
			//}

			// Create an instance of the open file dialog box.
			OpenFileDialog openFileDialog = new OpenFileDialog();

			// Call the ShowDialog method to show the dialog box. Process input if the user clicked OK.
			if (openFileDialog.ShowDialog() == false)
			{
				JObjectsEnumerable = new List<JObject>();
				return;
				
			}
			if (string.IsNullOrWhiteSpace(openFileDialog.FileName))
			{
				JObjectsEnumerable = new List<JObject>();
				return;
			}

			string[] fileAsStringArray = File.ReadAllLines(openFileDialog.FileName);
			JObjectsEnumerable = fileAsStringArray.Select(JsonConvert.DeserializeObject<JObject>).ToList();

			_totalRecordCount = JObjectsEnumerable.Count();

			IsHydrated = true;
		}
		
		public static IJEnumerable<JObject> ApplySort(string sortColumn)
		{
			// if we clicked on the same column twice, we reverse the order
			if (!string.IsNullOrEmpty(_previousSortColumn) && _previousSortColumn.Equals(sortColumn))
				_isAscending = !_isAscending;
			else
			{
				_isAscending = true;
				_previousSortColumn = sortColumn;
			}

			if (!HasMultipleObjects)
			{
				JObjectsEnumerable = _isAscending
						? JObjectsEnumerable.OrderBy(x => x.Property(sortColumn).ToString()).ToList()
						: JObjectsEnumerable.OrderByDescending(x => x.Property(sortColumn).ToString()).ToList();
			}
			else
			{
				// danger here...fortunately we won't execute yet
				JObjectsEnumerable = CleanSort(sortColumn, _isAscending).ToList();
			}
			
			return FirstPage();
		}
		
		public static IJEnumerable<JObject> NextPage()
		{
			_startRowIndex += _maximumRows;
			if (_startRowIndex + _maximumRows > _totalRecordCount)
				_maximumRows = _totalRecordCount - _startRowIndex;
			else
				_maximumRows = UserDefinedPageSize;

			return Paginate(_startRowIndex, _maximumRows);
		}
		
		public static IJEnumerable<JObject> PreviousPage()
		{
			_maximumRows = UserDefinedPageSize;
			_startRowIndex -= _maximumRows;
			return Paginate(_startRowIndex, _maximumRows);
		}

		public static IJEnumerable<JObject> FirstPage()
		{
			_maximumRows = UserDefinedPageSize;
			_startRowIndex = _defaultStartRowIndex;
			return Paginate(_startRowIndex, _maximumRows);
		}
		
		public static IJEnumerable<JObject> LastPage()
		{
			_startRowIndex = _totalRecordCount - (_totalRecordCount%_maximumRows == 0 ? _maximumRows : _totalRecordCount%_maximumRows);
			_maximumRows = _totalRecordCount - _startRowIndex;
			return Paginate(_startRowIndex, _maximumRows);
		}
		
		public static bool HasPreviousPage()
		{
			return _startRowIndex + 1 > _maximumRows; 
		}

		public static bool HasNextPage()
		{
			return _totalRecordCount > _startRowIndex + _maximumRows; 
		}

		public static string PageNavigationString()
		{
			return (_startRowIndex + 1) + " to " + (_startRowIndex + _maximumRows) + " of " + _totalRecordCount;
		}

		private static IEnumerable<JObject> CleanSort(string sortColumn, bool isAscending)
		{
			var hasProperty = JObjectsEnumerable.Where(o => o.Properties().FirstOrDefault(p => p.Name.Equals(sortColumn)) != null);
			var enumerable = hasProperty as IList<JObject> ?? hasProperty.ToList();
			var notHasProperty = JObjectsEnumerable.Where(o => o.Properties().FirstOrDefault(p => p.Name.Equals(sortColumn)) == null);

			hasProperty = isAscending
				? enumerable.OrderBy(x => x.Property(sortColumn).ToString())
				: enumerable.OrderByDescending(x => x.Property(sortColumn).ToString());

			return hasProperty.Concat(notHasProperty);
		}

		private static IJEnumerable<JObject> Paginate(int start, int pageSize)
		{
			var paginatedData = new List<JObject>();

			for (int i = 0; i < pageSize; i++)
			{
				paginatedData.Add(JObjectsEnumerable[start+i]);
			}
			return paginatedData.AsJEnumerable();
		}

		public static IJEnumerable<JObject> RefreshPage()
		{
			_maximumRows = UserDefinedPageSize;
			// check if we're close to last page
			if (_startRowIndex + _maximumRows >= _totalRecordCount)
				return LastPage();

			return Paginate(_startRowIndex, _maximumRows);
		}
	}
}