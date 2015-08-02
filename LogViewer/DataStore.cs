﻿#define DEBUG

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
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
        #region Private Fields

		private static List<JObject> JObjectsEnumerable;
		private static int _startRowIndex = 0;
		private static int _pageSize = 100;
		private static int _totalRecordCount = 0;
		private static string _previousSortColumn = "";
		private static bool _isAscending = true;
		private static int _defaultStartRowIndex = 0; // can be updated by section
		private static string _currentFileName;

        #endregion

		public static bool HasMultipleObjects = false;
		public static bool IsHydrated = false;
		public static bool IsReadingFile = false;
		public static int UserDefinedPageSize = 50;
		public static bool IsLargeFile = false;

	    public static int CurrentStartIndex 
	    {
	        get {return _startRowIndex; }
	    }
        
		public static int TotalRecordCount
		{
			get { return _totalRecordCount; }
			set { _totalRecordCount = value; }
		}

		public static void LoadFile()
		{
			// Create an instance of the open file dialog box.
			OpenFileDialog openFileDialog = new OpenFileDialog();
			JObjectsEnumerable = new List<JObject>();

			// Call the ShowDialog method to show the dialog box. Process input if the user clicked OK.
			if (openFileDialog.ShowDialog() == false) return;
			if (string.IsNullOrWhiteSpace(openFileDialog.FileName)) return;
			_currentFileName = openFileDialog.FileName;

			// read just enough to paint the screen
			StreamReader streamReader = File.OpenText(openFileDialog.FileName);
			IsReadingFile = true;
			string line = String.Empty;
			_totalRecordCount = 0;
			while ( _totalRecordCount < _pageSize*2  && (line = streamReader.ReadLine()) != null )
			{
				JObjectsEnumerable.Add(JsonConvert.DeserializeObject<JObject>(line));
				_totalRecordCount++;
			}

			// get rest of count
			while (streamReader.ReadLine() != null) _totalRecordCount++;
			
			IsLargeFile = (_totalRecordCount > 1000);
			IsHydrated = true;

			// spin off a thread to read rest of file
			if (IsLargeFile)
			{
				//Cache.InitCache.Start();
				Cache.InitCache.Start();
				IsReadingFile = false;
			}
			else
			{
				Task readRestofFile = Task.Factory.StartNew(ReadRestofFileT);
			}
		}

		private static void ReadRestofFileT()
		{
			StreamReader streamReader = File.OpenText(_currentFileName);
			string line = String.Empty;
			int i = 0;
			// lets make up for first read rows
			while (i < _pageSize*2 && streamReader.ReadLine() != null) i++; // because we read the line even on the 199th iteration
			while ((line = streamReader.ReadLine()) != null)
			{
				JObjectsEnumerable.Add(JsonConvert.DeserializeObject<JObject>(line));
			
			}
			streamReader.Close();
			IsReadingFile = false;
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
		
		public static IJEnumerable<JObject> FirstPage()
		{
			_pageSize = UserDefinedPageSize;
			_startRowIndex = _defaultStartRowIndex;

			return IsLargeFile ? Cache.First(_startRowIndex).AsJEnumerable() : Paginate(_startRowIndex, _pageSize);
		}
		
        public static IJEnumerable<JObject> PreviousPage()
		{
			_pageSize = UserDefinedPageSize;
			_startRowIndex -= _pageSize;

			return IsLargeFile ? Cache.Previous(_startRowIndex).AsJEnumerable() : Paginate(_startRowIndex, _pageSize);
		}

		public static IJEnumerable<JObject> NextPage()
		{
			_startRowIndex += _pageSize;
			if (_startRowIndex + _pageSize > _totalRecordCount)
				_pageSize = _totalRecordCount - _startRowIndex;
			else
				_pageSize = UserDefinedPageSize;
			
			return IsLargeFile ? Cache.Next(_startRowIndex).AsJEnumerable() : Paginate(_startRowIndex, _pageSize);
		}
		
		public static IJEnumerable<JObject> LastPage()
		{
			_startRowIndex = _totalRecordCount - (_totalRecordCount%_pageSize == 0 ? _pageSize : _totalRecordCount%_pageSize);
			_pageSize = _totalRecordCount - _startRowIndex;

			return IsLargeFile ? Cache.Last(_startRowIndex).AsJEnumerable() : Paginate(_startRowIndex, _pageSize);
		}
		
		public static bool HasPreviousPage()
		{
			return _startRowIndex + 1 > _pageSize; 
		}

		public static bool HasNextPage()
		{
			return _totalRecordCount > _startRowIndex + _pageSize; 
		}

		public static bool HasLastPage()
		{
			if (IsReadingFile)
				return false;
			return _totalRecordCount > _startRowIndex + _pageSize;
		}

		public static string PageNavigationString()
		{
			return (_startRowIndex + 1) + " to " + (_startRowIndex + _pageSize) + " of " + _totalRecordCount;
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
            // consider removing pageSize as parameter
			var paginatedData = new List<JObject>();

			for (int i = 0; i < pageSize; i++)
			{
				paginatedData.Add(JObjectsEnumerable[start+i]);
			}
			return paginatedData.AsJEnumerable();
		}

		public static IJEnumerable<JObject> ResizeCurrentPage()
		{
			_pageSize = UserDefinedPageSize;
			// check if we're close to last page
			if (_startRowIndex + _pageSize >= _totalRecordCount)
				return LastPage();

			return Paginate(_startRowIndex, _pageSize);
		}

		public static class Cache
        {
            #region Private Fields

            private static Dictionary<string, List<JObject>> _cache;
			private static string _lastRequestPage;
		    private static int _lastRequestedStart;
			private static Task refreshTask = new Task(RefreshCache);
			private static int _fileCurrentPosition = 0;
			private const string first = "first";
			private const string previous = "previous";
			private const string next = "next";
			private const string last = "last";
			private static StreamReader streamReader;

            #endregion
            
            public static Task InitCache = new Task(PopulateCache);
			
			public static List<JObject> First(int startRowIndex)
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

            public static List<JObject> Previous(int startRowIndex)
			{
				if (refreshTask.Status == TaskStatus.Running)
					refreshTask.Wait();

                _lastRequestPage = previous;
                _lastRequestedStart = startRowIndex;

				var requestedPage = _cache[_lastRequestPage];
                if (_lastRequestedStart < _pageSize )
                    requestedPage = _cache[first];


				refreshTask = new Task(RefreshCache);
				refreshTask.Start();

				return requestedPage;
			}

            public static List<JObject> Next(int startRowIndex)
			{
				if (refreshTask.Status == TaskStatus.Running)
					refreshTask.Wait();

                _lastRequestPage = next;
                _lastRequestedStart = startRowIndex;

                var requestedPage = _cache[_lastRequestPage];

                if(_lastRequestedStart + _pageSize >= _totalRecordCount)
				    requestedPage = _cache[last];

				refreshTask = new Task(RefreshCache);
				refreshTask.Start();

				return requestedPage;
			}

            public static List<JObject> Last(int startRowIndex)
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

			public static void PopulateCache()
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
			private static void RefreshCache()
			{
				InitCache.Wait();

				switch (_lastRequestPage)
				{
					case first :
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
			private static List<JObject> ContinueRead()
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
			private static List<JObject> ReadAtIndex(int startIndex, int pageSize)
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
		}
	}
}