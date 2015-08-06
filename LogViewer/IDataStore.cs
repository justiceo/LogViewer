using Newtonsoft.Json.Linq;

namespace LogViewer
{
    /// <summary>
    /// Exposes the basic functionalities of dataStore
    /// </summary>
    public interface IDataStore
    {
	    int GetCurrentStartIndex();

	    int GetTotalRecordCount();

	    string GetPageNavigationString();

	    IJEnumerable<JObject> GetPage(string page);

	    bool HasPage(string page);

	    IJEnumerable<JObject> ResizePage();

	    IJEnumerable<JObject> SortBy(string sortColumn);
	    
    }
}
