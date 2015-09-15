#define DEBUG

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogViewer
{
	public class DataStore : IDataStore
	{
		#region Private & Protected Fields
		
		protected readonly string SourceFileName;
		protected int TotalRecordCount;
		public int DefaultStartRowIndex;
		protected int StartRowIndex = 0;
		protected int PageSize = 50;

		private List<JObject> _jObjectsEnumerable;
		private string _currentSortColumn = "";
		private bool _isSortOrderAscending = true;
		private int _largeFileEntireLinesCount;

		#endregion

		#region const

		public const string First = "first";
		public const string Previous = "previous";
		public const string Next = "next";
		public const string Last = "last";

		#endregion

		#region Public Variables

		public bool HasMultipleObjects = false;
		public bool IsReadingFile;
		public int UserDefinedPageSize = 50;
		public bool IsLargeFile;
		public int DocSectionSize;

		#endregion

		public DataStore(string sourceFileName, int startIndex)
		{
			SourceFileName = sourceFileName;
			_jObjectsEnumerable = new List<JObject>();
			DefaultStartRowIndex = startIndex;
			TotalRecordCount = 0;
			DocSectionSize = 10000;
		}

		public virtual void LoadFile() {

			// read just enough to paint the screen
			StreamReader streamReader = File.OpenText(SourceFileName);
			IsReadingFile = true;
			string line = String.Empty;
			while (TotalRecordCount < PageSize * 2 && (line = streamReader.ReadLine()) != null)
			{
				_jObjectsEnumerable.Add(JsonConvert.DeserializeObject<JObject>(line));
				TotalRecordCount++;
			}

			// get rest of count and close stream
			while (streamReader.ReadLine() != null) TotalRecordCount++;
			streamReader.Close();

			// determine if large file and split into sections
			IsLargeFile = TotalRecordCount > DocSectionSize;
			if (IsLargeFile)
			{
				_largeFileEntireLinesCount = TotalRecordCount;
				TotalRecordCount = DocSectionSize;
			}

			// spin off a thread to read rest of file
			Task readRestofFile = new Task(ReadRestofFileT);
			readRestofFile.Start();
		}
		
		public int GetCurrentStartIndex()
		{
			return StartRowIndex; 
		}

		public int GetTotalRecordCount()
		{
			return TotalRecordCount;
		}
		
		public string GetPageNavigationString()
		{
		    var from = StartRowIndex + 1;
		    var to = StartRowIndex + PageSize;
            
            to = (to < TotalRecordCount) ? to : TotalRecordCount;
		    from = (from < to) ? from : to;

		    if (TotalRecordCount == 0)
		        return "No records";

			return from + " to " + to + " of " + TotalRecordCount;
		}

		public virtual IJEnumerable<JObject> GetPage(string page)
		{
			switch (page)
			{
				case First:
					PageSize = UserDefinedPageSize;
					StartRowIndex = DefaultStartRowIndex;
					break;
				case Previous:
					PageSize = UserDefinedPageSize;
					StartRowIndex -= PageSize;
					break;
				case Next:
					StartRowIndex += PageSize;
					if (StartRowIndex + PageSize > TotalRecordCount)
						PageSize = TotalRecordCount - StartRowIndex;
					else
						PageSize = UserDefinedPageSize;
					break;
				case Last:
					StartRowIndex = TotalRecordCount - (TotalRecordCount % PageSize == 0 ? PageSize : TotalRecordCount % PageSize);
					PageSize = TotalRecordCount - StartRowIndex;
					break;
			}

			return GetPageAt(StartRowIndex, PageSize);
		}

		public bool HasPage(string page)
		{
			switch (page)
			{
				case First:
					return StartRowIndex > DefaultStartRowIndex;
				case Previous:
					return StartRowIndex > DefaultStartRowIndex;
				case Next:
					return TotalRecordCount > StartRowIndex + PageSize;
				case Last:
					if (IsReadingFile)
						return false;
					return TotalRecordCount > StartRowIndex + PageSize;
				default:
					return false;
			}
		}

		public virtual IJEnumerable<JObject> ResizePage()
		{
			PageSize = UserDefinedPageSize;

			// check if we're close to last page
			if (StartRowIndex + PageSize >= TotalRecordCount)
				return GetPage(Last);

			return GetPageAt(StartRowIndex, PageSize);
		}

	    public virtual IJEnumerable<JObject> ReverseOrder()
	    {
	        _jObjectsEnumerable.Reverse();

	        return _jObjectsEnumerable.AsJEnumerable();
	    } 
		
		public virtual IJEnumerable<JObject> SortBy(string sortColumn)
		{
			// if we clicked on the same column twice, we reverse the order
			if (!string.IsNullOrEmpty(_currentSortColumn) && _currentSortColumn.Equals(sortColumn))
				_isSortOrderAscending = !_isSortOrderAscending;
			else
			{
				_isSortOrderAscending = true;
				_currentSortColumn = sortColumn;
			}
			
			// perform the sorting
			_jObjectsEnumerable = _isSortOrderAscending
					? _jObjectsEnumerable.OrderBy(x => x.Property(sortColumn).ToString()).ToList()
					: _jObjectsEnumerable.OrderByDescending(x => x.Property(sortColumn).ToString()).ToList();
			
			return GetPage(First);
		}

		protected virtual IJEnumerable<JObject> GetPageAt(int start, int pageSize)
		{
			// consider removing pageSize as parameter
			var paginatedData = new List<JObject>();

			// start just can't be less than 0
			if (start < 0)
				start = StartRowIndex = 0;

			// and start + pageSize cannot be greater than totalRecordCount
			if (start + pageSize > TotalRecordCount)
				pageSize = TotalRecordCount - start;

			for (int i = 0; i < pageSize; i++)
			{
				paginatedData.Add(_jObjectsEnumerable[start + i]);
			}
			return paginatedData.AsJEnumerable();
		}
		
		private void ReadRestofFileT()
		{
			// prepare errand variables
			StreamReader streamReader = File.OpenText(SourceFileName);
			string line = String.Empty;
			int counter = 0;

			// lets make up for first read rows
			while (counter < PageSize * 2 && streamReader.ReadLine() != null) counter++;

			if (IsLargeFile) // Just read first chunk
			{
				while (counter < DocSectionSize && (line = streamReader.ReadLine()) != null)
				{
					_jObjectsEnumerable.Add(JsonConvert.DeserializeObject<JObject>(line));
					counter++;
				}
			}
			else
			{
				while ((line = streamReader.ReadLine()) != null)
				{
					_jObjectsEnumerable.Add(JsonConvert.DeserializeObject<JObject>(line));
				}
			}
			
			
			streamReader.Close();
			IsReadingFile = false;
		}


		public virtual IJEnumerable<JObject> Filter(List<FilterObject> filterCriteria)
		{
			// perform the filtering
			foreach (var filterCriterion in filterCriteria)
			{
				_jObjectsEnumerable = _jObjectsEnumerable.Where(c => c.GetValue(filterCriterion.ColumnName).ToString().Contains(filterCriterion.FilterCriteria)).ToList();
			}

			TotalRecordCount = _jObjectsEnumerable.Count();

			return GetPage(First);
		}
	}
}