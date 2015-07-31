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
		private static string _currentFileName;

		public static bool HasMultipleObjects = false;
		public static bool IsHydrated = false;
		public static bool IsReadingFile = false;
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
			JObjectsEnumerable = new List<JObject>();

			// Call the ShowDialog method to show the dialog box. Process input if the user clicked OK.
			if (openFileDialog.ShowDialog() == false) return;
			if (string.IsNullOrWhiteSpace(openFileDialog.FileName)) return;
			_currentFileName = openFileDialog.FileName;


			IsReadingFile = true;
			// read just enough to paint the screen
			StreamReader streamReader = File.OpenText(openFileDialog.FileName);
			string line = String.Empty;
			int i = 0;
			while ((line = streamReader.ReadLine()) != null && i < _maximumRows*2 )
			{
				JObjectsEnumerable.Add(JsonConvert.DeserializeObject<JObject>(line));
				i++;
			}
			//streamReader.Close();

			_totalRecordCount = JObjectsEnumerable.Count();
			IsHydrated = true;

			// spin off a thread to read rest of file
			Task readRestofFile = Task.Factory.StartNew(() => ReadRestofFileT());
			//readRestofFile.Start();
			//readRestofFile.Wait();
		}

		public static void ReadRestofFileT()
		{
			StreamReader streamReader = File.OpenText(_currentFileName);
			string line = String.Empty;
			int i = 0;
			// lets make up for first read rows
			while (streamReader.ReadLine() != null && i < _maximumRows*2 -1) i++; // because we read the line even on the 199th iteration
			while ((line = streamReader.ReadLine()) != null)
			{
				JObjectsEnumerable.Add(JsonConvert.DeserializeObject<JObject>(line));
			
			}
			streamReader.Close();
			IsReadingFile = false;
			_totalRecordCount = JObjectsEnumerable.Count();
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
			if(IsReadingFile)
				return (_startRowIndex + 1) + " to " + (_startRowIndex + _maximumRows) + " of ....";
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