using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogViewer
{
	public class CacheDataStore : DataStore
	{
		private Dictionary<string, List<JObject>> _cache;
		private string _lastRequestPage;
		private int _lastRequestedStart;
		private Task _refreshTask;
		private int _fileCurrentPosition = 0;
		private StreamReader _streamReader; 
		
		public Task InitCache;

		public CacheDataStore(string sourceFileName, int startIndex) : base(sourceFileName, startIndex)
		{
			_cache = new Dictionary<string, List<JObject>>();
		}
		public void LoadFile()
		{
			// load first and last pages into cache
			_cache[First] = GetPageAt(DefaultStartRowIndex, PageSize);
			var lastPageStart = TotalRecordCount - (TotalRecordCount % PageSize == 0 ? PageSize : TotalRecordCount % PageSize);
			_cache[Last] = GetPageAt(lastPageStart, TotalRecordCount - lastPageStart);

			// instantiate the rest
			_cache[Next] = new List<JObject>();
			_cache[Previous] = new List<JObject>();
		}

		public List<JObject> GetPage(string page)
		{
			InitCache.Wait();

			if (_refreshTask.Status == TaskStatus.Running)
				_refreshTask.Wait();

			_lastRequestPage = page;

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


			_lastRequestedStart = StartRowIndex;

			var requestedPage = _cache[_lastRequestPage];
			if (_lastRequestedStart < PageSize) // previous
				requestedPage = _cache[First];
			if (_lastRequestedStart + PageSize >= TotalRecordCount) // next
				requestedPage = _cache[Last];


			_refreshTask = new Task(RefreshCache);
			_refreshTask.Start();

			return requestedPage;
		}

		/// <summary>
		/// Refreshes the cache as user navigates the pages
		/// </summary>
		private void RefreshCache()
		{
			InitCache.Wait();

			switch (_lastRequestPage)
			{
				case First:
					_cache[Next] = GetPageAt(_lastRequestedStart + PageSize, PageSize);
					break;
				case Previous:
					_cache[Next] = _cache[Previous];
					_cache[Previous] = GetPageAt(_lastRequestedStart - PageSize, PageSize);
					break;
				case Next:
					_cache[Previous] = _cache[Next];
					_cache[Next] = GetPageAt(_lastRequestedStart + PageSize, PageSize);
					break;
				case Last:
					_cache[Previous] = GetPageAt(_lastRequestedStart - UserDefinedPageSize, UserDefinedPageSize);
					break;
			}
		}

		public List<JObject> ResizePage()
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
				return GetPage(Last);

			return GetPageAt(StartRowIndex, PageSize);
		}

		private List<JObject> GetPageAt(int startIndex, int pageSize)
		{
			List<JObject> jObjectsList = new List<JObject>();

			if (_streamReader == null || _streamReader.EndOfStream)
				_streamReader = File.OpenText(SourceFileName);

			// if we're requesting the first or last page, return them from cache
			if (startIndex < pageSize && _cache.ContainsKey(First))
				return _cache[First];
			if (startIndex + pageSize >= TotalRecordCount && _cache.ContainsKey(Last))
				return _cache[Last];

			// set start position for read
			if (_fileCurrentPosition > startIndex)
			{
				// restart stream
				_streamReader.Close();
				_streamReader = File.OpenText(SourceFileName);
				_fileCurrentPosition = 0;
			}
			while (_fileCurrentPosition < startIndex && _streamReader.ReadLine() != null)
			{
				_fileCurrentPosition++;
			}

			// perform the read and convert
			string line;
			while ((_fileCurrentPosition < startIndex + pageSize) && (line = _streamReader.ReadLine()) != null)
			{
				jObjectsList.Add(JsonConvert.DeserializeObject<JObject>(line));
				_fileCurrentPosition++;
			}

			return jObjectsList;
		}

		
	}
}
