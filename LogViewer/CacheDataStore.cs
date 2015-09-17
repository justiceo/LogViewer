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
        private Dictionary<Page, IJEnumerable<JObject>> _cache;
		private Page _lastRequestPage;
		private int _lastRequestedStart;
		private Task _refreshTask;
		private int _fileCurrentPosition = 0;
		private StreamReader _streamReader; 
		

		public CacheDataStore(string sourceFileName, int startIndex) : base(sourceFileName, startIndex)
		{
            _cache = new Dictionary<Page, IJEnumerable<JObject>>();
		}
		public override void LoadFile()
		{
            // get doc size
		    _streamReader = File.OpenText(SourceFileName);
            while (_streamReader.ReadLine() != null) TotalRecordCount++;

			// load first and last pages into cache
			_cache[Page.First] = GetPageAt(DefaultStartRowIndex, PageSize);
			var lastPageStart = TotalRecordCount - (TotalRecordCount % PageSize == 0 ? PageSize : TotalRecordCount % PageSize);
			_cache[Page.Last] = GetPageAt(lastPageStart, TotalRecordCount - lastPageStart);

			// instantiate the rest
			_cache[Page.Next] = new JEnumerable<JObject>();
			_cache[Page.Previous] = new JEnumerable<JObject>();

			_lastRequestPage = Page.First;
            _refreshTask = new Task(RefreshCache);
            _refreshTask.Start();
		}

        public override IJEnumerable<JObject> GetPage(Page page)
		{
			if (_refreshTask.Status == TaskStatus.Running || _refreshTask.Status == TaskStatus.WaitingForActivation || _refreshTask.Status == TaskStatus.WaitingToRun)
				_refreshTask.Wait();

			_lastRequestPage = page;

			switch (page)
			{
				case Page.First:
					PageSize = UserDefinedPageSize;
					StartRowIndex = DefaultStartRowIndex;
					break;
				case Page.Previous:
					PageSize = UserDefinedPageSize;
					StartRowIndex -= PageSize;
					break;
				case Page.Next:
					StartRowIndex += PageSize;
					if (StartRowIndex + PageSize > TotalRecordCount)
						PageSize = TotalRecordCount - StartRowIndex;
					else
						PageSize = UserDefinedPageSize;
					break;
				case Page.Last:
					StartRowIndex = TotalRecordCount - (TotalRecordCount % PageSize == 0 ? PageSize : TotalRecordCount % PageSize);
					PageSize = TotalRecordCount - StartRowIndex;
					break;
			}


			_lastRequestedStart = StartRowIndex;

			var requestedPage = _cache[_lastRequestPage];
			if (_lastRequestedStart < PageSize) // previous
				requestedPage = _cache[Page.First];
			if (_lastRequestedStart + PageSize >= TotalRecordCount) // next
				requestedPage = _cache[Page.Last];


			_refreshTask = new Task(RefreshCache);
			_refreshTask.Start();

			return requestedPage;
		}

		/// <summary>
		/// Refreshes the cache as user navigates the pages
		/// </summary>
		private void RefreshCache()
		{
			switch (_lastRequestPage)
			{
				case Page.First:
					_cache[Page.Next] = GetPageAt(_lastRequestedStart + PageSize, PageSize);
					break;
				case Page.Previous:
					_cache[Page.Next] = _cache[Page.Previous];
					_cache[Page.Previous] = GetPageAt(_lastRequestedStart - PageSize, PageSize);
					break;
				case Page.Next:
					_cache[Page.Previous] = _cache[Page.Next];
					_cache[Page.Next] = GetPageAt(_lastRequestedStart + PageSize, PageSize);
					break;
				case Page.Last:
					_cache[Page.Previous] = GetPageAt(_lastRequestedStart - UserDefinedPageSize, UserDefinedPageSize);
					break;
			}
		}

		public override IJEnumerable<JObject> ResizePage()
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
				return GetPage(Page.Last);

			return GetPageAt(StartRowIndex, PageSize);
		}

        protected override IJEnumerable<JObject> GetPageAt(int startIndex, int pageSize)
		{
			List<JObject> jObjectsList = new List<JObject>();
            
            if (_streamReader == null)
            {
                _streamReader = new StreamReader(SourceFileName);
            }
            if (_streamReader.EndOfStream)
            {
                _streamReader.Close();
                
                _streamReader = new StreamReader(SourceFileName);
            }
            // if we're requesting the first or last page, return them from cache
			if (startIndex < pageSize && _cache.ContainsKey(Page.First))
				return _cache[Page.First];
			if (startIndex + pageSize >= TotalRecordCount && _cache.ContainsKey(Page.Last))
				return _cache[Page.Last];

			// set start position for read
			if (_fileCurrentPosition > startIndex)
			{
				// restart stream
                _streamReader.Close();
				_streamReader = new StreamReader(SourceFileName);
				_fileCurrentPosition = 0;
			}
            string line;
			while (_fileCurrentPosition < startIndex && _streamReader.ReadLine() != null)
			{
				_fileCurrentPosition++;
			}

			// perform the read and convert
			while ((_fileCurrentPosition < startIndex + pageSize) && (line = _streamReader.ReadLine()) != null)
			{
				jObjectsList.Add(JsonConvert.DeserializeObject<JObject>(line));
				_fileCurrentPosition++;
			}

			return jObjectsList.AsJEnumerable();
		}

		
	}
}
