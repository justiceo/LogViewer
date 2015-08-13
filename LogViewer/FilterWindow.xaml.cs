﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LogViewer
{
    /// <summary>
    /// Interaction logic for FilterWindow.xaml
    /// </summary>
    public partial class FilterWindow : Window
    {
        public FilterWindow(List<string> columnNames )
        {
            InitializeComponent();
			DataContext = new FilterViewModel(columnNames);
        }

	    public List<FilterObject> GetFilterObjectsList()
	    {
			return new List<FilterObject>();
	    }
    }
}