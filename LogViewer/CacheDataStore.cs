using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace LogViewer
{
	public class CacheDataStore : DataStore
	{
		private List<JObject> _jObjectsEnumerable;

		public CacheDataStore(string sourceFileName, int startIndex) : base(sourceFileName, startIndex)
		{
			_jObjectsEnumerable = new List<JObject>();
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
	}
}
