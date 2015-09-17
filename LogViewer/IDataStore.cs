using Newtonsoft.Json.Linq;

namespace LogViewer
{
    /// <summary>
    /// Exposes the basic functionalities of dataStore
    /// </summary>
    public interface IDataStore
    {
	    void LoadFile();

	    int GetCurrentStartIndex();

	    int GetTotalRecordCount();

	    string GetPageNavigationString();

	    IJEnumerable<JObject> GetPage(Page page);

	    bool HasPage(Page page);

	    IJEnumerable<JObject> ResizePage();

	    IJEnumerable<JObject> SortBy(string sortColumn);
	    
    }
}
