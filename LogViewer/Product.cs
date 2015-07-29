using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogViewer
{
	public class Product
	{

		public int ID { get; set; }

		public string Name { get; set; }

		public Product(int id, string name)
		{
			ID = id;
			Name = name;
		}
	}
}
