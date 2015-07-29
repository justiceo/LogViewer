using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;

namespace LogViewer
{
	/// <summary>
	/// A custom list to give BindingList a sortable behavior
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class JObjectBindingList : BindingList<JObject>
	{

		// reference to the list provided at the time of instantiation
		private List<JObject> _copyOfBaseList;

		// function that refereshes the contents of the base classes collection of elements
		private readonly Action<JObjectBindingList, List<JObject>> _populateBaseList = (a, b) => a.ResetItems(b);

		public bool HasMultipleObjects = true;

		public int PageSize = 500;

		public IEnumerable<JProperty> FirstObjectJProperties
		{
			get
			{
				var firstOrDefault = this.FirstOrDefault();
				if (firstOrDefault != null) return firstOrDefault.Properties();
				return null;
			}
		}

		public JObjectBindingList()
		{
			RaiseListChangedEvents = false;
			_copyOfBaseList = new List<JObject>();
		}

		public JObjectBindingList(IEnumerable<JObject> enumerable)
		{
			RaiseListChangedEvents = false;
			_copyOfBaseList = enumerable.ToList();
			_populateBaseList(this, _copyOfBaseList);
		}

		protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
		{
			if (!HasMultipleObjects)
			{
				try
				{
					_copyOfBaseList = direction.Equals(ListSortDirection.Ascending)
						? _copyOfBaseList.OrderBy(x => x.Property(prop.Name).ToString()).ToList()
						: _copyOfBaseList.OrderByDescending(x => x.Property(prop.Name).ToString()).ToList();
				}
				catch (NullReferenceException)
				{
					HasMultipleObjects = true;
					_copyOfBaseList = CleanSort(prop, direction);
				}
			}
			else
			{
				_copyOfBaseList = CleanSort(prop, direction);
			}

			ResetItems(_copyOfBaseList);
			ResetBindings();

		}

		private List<JObject> CleanSort(PropertyDescriptor prop, ListSortDirection direction)
		{
			var hasProperty = _copyOfBaseList.Where(o => o.Properties().FirstOrDefault(p => p.Name.Equals(prop.Name)) != null);
			var enumerable = hasProperty as IList<JObject> ?? hasProperty.ToList();
			var notHasProperty = _copyOfBaseList.Where(o => o.Properties().FirstOrDefault(p => p.Name.Equals(prop.Name)) == null);

			hasProperty = direction.Equals(ListSortDirection.Ascending)
				? enumerable.OrderBy(x => x.Property(prop.Name).ToString())
				: enumerable.OrderByDescending(x => x.Property(prop.Name).ToString());

			return hasProperty.Concat(notHasProperty).ToList();
		}

		protected override void RemoveSortCore()
		{
			ResetItems(_copyOfBaseList);
		}

		internal void ResetItems(List<JObject> items)
		{
			ClearItems();
			items.ForEach(Add);
		}

		protected override bool SupportsSortingCore
		{
			get
			{
				return true;
			}
		}

		public string TranslateProperties(List<string> list)
		{
			list = list.ConvertAll(s => "\"" + s + "\":\"\"");
			var jobjectStr = string.Join(", ", list);
			jobjectStr.Remove(jobjectStr.LastIndexOf(','));
			jobjectStr = "{" + jobjectStr + "}";
			return jobjectStr;
		}

	}


}


