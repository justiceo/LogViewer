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
		protected readonly int TotalRecordCount;
		protected readonly int DefaultStartRowIndex;
		protected int StartRowIndex = 0;
		protected int PageSize = 100;

		private List<JObject> _jObjectsEnumerable;
		private string _currentSortColumn = "";
		private bool _isSortOrderAscending = true;

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

		#endregion

		public DataStore(string sourceFileName, int startIndex)
		{
			SourceFileName = sourceFileName;
			_jObjectsEnumerable = new List<JObject>();
			DefaultStartRowIndex = startIndex;

			// read just enough to paint the screen
			StreamReader streamReader = File.OpenText(SourceFileName);
			IsReadingFile = true;
			string line = String.Empty;
			TotalRecordCount = 0;
			while (TotalRecordCount < PageSize * 2 && (line = streamReader.ReadLine()) != null)
			{
				_jObjectsEnumerable.Add(JsonConvert.DeserializeObject<JObject>(line));
				TotalRecordCount++;
			}

			// get rest of count and close stream
			while (streamReader.ReadLine() != null) TotalRecordCount++;
			streamReader.Close();

			// determine if large file and split into sections
			IsLargeFile = false; // (_totalRecordCount > 1000);

			// spin off a thread to read rest of file
			if (IsLargeFile)
			{
				//_cacheStore.InitCache = new Task(_cacheStore.PopulateCache);
				//_cacheStore.InitCache.Start();
				//IsReadingFile = false;
			}
			else
			{
				Task readRestofFile = new Task(ReadRestofFileT);
				readRestofFile.Start();
			}
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
			return (StartRowIndex + 1) + " to " + (StartRowIndex + PageSize) + " of " + TotalRecordCount;
		}

		public IJEnumerable<JObject> GetPage(string page)
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

		public IJEnumerable<JObject> ResizePage()
		{
			PageSize = UserDefinedPageSize;

			// for large files just re-init the cache to propagate page sizes across the cached versions
			if (IsLargeFile)
			{
				//_startRowIndex = _defaultStartRowIndex;

				//            _cacheStore.InitCache = new Task(_cacheStore.PopulateCache);
				//            _cacheStore.InitCache.Start();
				//return _cacheStore.First(_startRowIndex).AsJEnumerable();
			}

			// check if we're close to last page
			if (StartRowIndex + PageSize >= TotalRecordCount)
				return GetPage("last");

			return GetPageAt(StartRowIndex, PageSize);
		}
		
		public IJEnumerable<JObject> SortBy(string sortColumn)
		{
			// if we clicked on the same column twice, we reverse the order
			if (!string.IsNullOrEmpty(_currentSortColumn) && _currentSortColumn.Equals(sortColumn))
				_isSortOrderAscending = !_isSortOrderAscending;
			else
			{
				_isSortOrderAscending = true;
				_currentSortColumn = sortColumn;
			}

			if (!HasMultipleObjects)
			{
				_jObjectsEnumerable = _isSortOrderAscending
						? _jObjectsEnumerable.OrderBy(x => x.Property(sortColumn).ToString()).ToList()
						: _jObjectsEnumerable.OrderByDescending(x => x.Property(sortColumn).ToString()).ToList();
			}
			else
			{
				// danger here...fortunately we won't execute yet
				_jObjectsEnumerable = CleanSort(sortColumn, _isSortOrderAscending).ToList();
			}

			return GetPage(First);
		}

		private IJEnumerable<JObject> GetPageAt(int start, int pageSize)
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
				
		private IEnumerable<JObject> CleanSort(string sortColumn, bool isAscending)
		{
			var hasProperty = _jObjectsEnumerable.Where(o => o.Properties().FirstOrDefault(p => p.Name.Equals(sortColumn)) != null);
			var enumerable = hasProperty as IList<JObject> ?? hasProperty.ToList();
			var notHasProperty = _jObjectsEnumerable.Where(o => o.Properties().FirstOrDefault(p => p.Name.Equals(sortColumn)) == null);

			hasProperty = isAscending
				? enumerable.OrderBy(x => x.Property(sortColumn).ToString())
				: enumerable.OrderByDescending(x => x.Property(sortColumn).ToString());

			return hasProperty.Concat(notHasProperty);
		}

		private void ReadRestofFileT()
		{
			StreamReader streamReader = File.OpenText(SourceFileName);
			string line = String.Empty;
			int i = 0;
			// lets make up for first read rows
			while (i < PageSize * 2 && streamReader.ReadLine() != null) i++; // because we read the line even on the 199th iteration
			while ((line = streamReader.ReadLine()) != null)
			{
				_jObjectsEnumerable.Add(JsonConvert.DeserializeObject<JObject>(line));

			}
			streamReader.Close();
			IsReadingFile = false;
		}
		
		

		/*public class Cache : DataStore
		{
			#region Private Fields

			private Dictionary<string, List<JObject>> _cache;
			private string _lastRequestPage;
			private int _lastRequestedStart;
			private Task refreshTask;
			private int _fileCurrentPosition = 0;
			private const string first = "first";
			private const string previous = "previous";
			private const string next = "next";
			private const string last = "last";
			private StreamReader streamReader;

			#endregion

			public Task InitCache;

			public List<JObject> First(int startRowIndex)
			{
				InitCache.Wait();

				if (refreshTask.Status == TaskStatus.Running)
					refreshTask.Wait();

				_lastRequestPage = first;
				_lastRequestedStart = startRowIndex;

				var requestedPage = _cache[_lastRequestPage];
				refreshTask = new Task(RefreshCache);
				refreshTask.Start();

				return requestedPage;
			}

			public List<JObject> Previous(int startRowIndex)
			{
				if (refreshTask.Status == TaskStatus.Running)
					refreshTask.Wait();

				_lastRequestPage = previous;
				_lastRequestedStart = startRowIndex;

				var requestedPage = _cache[_lastRequestPage];
				if (_lastRequestedStart < base.GetPageSize())
					requestedPage = _cache[first];


				refreshTask = new Task(RefreshCache);
				refreshTask.Start();

				return requestedPage;
			}

			public List<JObject> Next(int startRowIndex)
			{
				if (refreshTask.Status == TaskStatus.Running)
					refreshTask.Wait();

				_lastRequestPage = next;
				_lastRequestedStart = startRowIndex;

				var requestedPage = _cache[_lastRequestPage];

				if (_lastRequestedStart + _pageSize >= _totalRecordCount)
					requestedPage = _cache[last];

				refreshTask = new Task(RefreshCache);
				refreshTask.Start();

				return requestedPage;
			}

			public List<JObject> Last(int startRowIndex)
			{
				if (refreshTask.Status == TaskStatus.Running)
					refreshTask.Wait();

				_lastRequestPage = last;
				_lastRequestedStart = startRowIndex;
				var requestedPage = _cache[_lastRequestPage];

				refreshTask = new Task(RefreshCache);
				refreshTask.Start();

				return requestedPage;
			}

			public void PopulateCache()
			{
				_cache = new Dictionary<string, List<JObject>>();
				// load first and last pages into cache
				_cache[first] = ReadAtIndex(_defaultStartRowIndex, _pageSize);
				var lastPageStart = _totalRecordCount - (_totalRecordCount % _pageSize == 0 ? _pageSize : _totalRecordCount % _pageSize);
				_cache[last] = ReadAtIndex(lastPageStart, _totalRecordCount - lastPageStart);

				// instantiate the rest
				_cache[next] = new List<JObject>();
				_cache[previous] = new List<JObject>();
			}

			/// <summary>
			/// Refreshes the cache as user navigates the pages
			/// </summary>
			private void RefreshCache()
			{
				InitCache.Wait();

				switch (_lastRequestPage)
				{
					case first:
						_cache[next] = ReadAtIndex(_lastRequestedStart + _pageSize, _pageSize);
						break;
					case previous:
						_cache[next] = _cache[previous];
						_cache[previous] = ReadAtIndex(_lastRequestedStart - _pageSize, _pageSize);
						break;
					case next:
						_cache[previous] = _cache[next];
						_cache[next] = ReadAtIndex(_lastRequestedStart + _pageSize, _pageSize);
						break;
					case last:
						_cache[previous] = ReadAtIndex(_lastRequestedStart - UserDefinedPageSize, UserDefinedPageSize);
						break;
				}
			}

			/// <summary>
			/// Returns the next maxCachableRows from a file
			/// </summary>
			/// <returns></returns>
			private List<JObject> ContinueRead()
			{
				List<JObject> jObjectsList = new List<JObject>();
				if (streamReader == null)
					streamReader = File.OpenText(_currentFileName);
				string line;
				int counter = 0;

				while ((line = streamReader.ReadLine()) != null && counter < _pageSize)
				{
					jObjectsList.Add(JsonConvert.DeserializeObject<JObject>(line));
					counter++;
				}
				_fileCurrentPosition += counter;
				return jObjectsList;
			}

			/// <summary>
			/// Returns the next maxCachableRows from a file, starting at specified index
			/// Useful for reading last pages, previous pages or reading from weird points in file
			/// </summary>
			/// <returns></returns>
			private List<JObject> ReadAtIndex(int startIndex, int pageSize)
			{
				List<JObject> jObjectsList = new List<JObject>();

				if (streamReader == null || streamReader.EndOfStream)
					streamReader = File.OpenText(_currentFileName);

				// if we're requesting the first or last page, return them from cache
				if (startIndex < pageSize && _cache.ContainsKey(first))
					return _cache[first];
				if (startIndex + pageSize >= _totalRecordCount && _cache.ContainsKey(last))
					return _cache[last];

				// set start position for read
				if (_fileCurrentPosition > startIndex)
				{
#if DEBUG
					Console.WriteLine("_fileCurrentPosition is " + _fileCurrentPosition + ", which is greater than startIndex: " + startIndex);
#endif

					// restart stream
					streamReader.Close();
					streamReader = File.OpenText(_currentFileName);
					_fileCurrentPosition = 0;
				}
				while (_fileCurrentPosition < startIndex && streamReader.ReadLine() != null)
				{
					_fileCurrentPosition++;
				}

#if DEBUG
				Console.WriteLine("fileCurrentPosition is finally set to: " + _fileCurrentPosition);
#endif

				// perform the read and convert
				string line;
				while ((_fileCurrentPosition < startIndex + pageSize) && (line = streamReader.ReadLine()) != null)
				{
					jObjectsList.Add(JsonConvert.DeserializeObject<JObject>(line));
					_fileCurrentPosition++;
				}

#if DEBUG
				Console.WriteLine("after read, fileCurrentPosition is: " + _fileCurrentPosition);
#endif
				return jObjectsList;
			}
		} */
	}
}