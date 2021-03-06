﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogViewer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
        
		public MainWindow()
		{
			InitializeComponent();
			DataContext = new MainViewModel();
		}

		/// <summary>
		/// Defers sorting to the Data Access layer and simply updates grid with results
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void LogDataGrid_OnSorting(object sender, DataGridSortingEventArgs e)
		{
			e.Handled = true;
			MainViewModel mainViewModel = (MainViewModel)DataContext;
			mainViewModel.Sort(e.Column.SortMemberPath);
		}

		/// <summary>
		/// Force sortable on all columns regardless of whether they're nullable.
		/// Since the sorting operation is abstracted to the Data Access layer which takes care of edge cases.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void LogDataGrid_OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
		{
            MainViewModel mainViewModel = (MainViewModel)DataContext;
            e.Column.CanUserSort = !mainViewModel.IsLargeFile();
		}
		
		/// <summary>
		/// Closes the application
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ExitHandler(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

	    private void LogDataGrid_OnLoadingRow(object sender, DataGridRowEventArgs e)
	    {
            MainViewModel mainViewModel = (MainViewModel)DataContext;
            e.Row.Header = (mainViewModel.StartRowIndex() + e.Row.GetIndex() + 1).ToString();
	    }

	    private void LogDataGrid_OnLoaded(object sender, RoutedEventArgs e)
	    {
	        // init page sizing
            PageSizeCombobox.SetBinding(ComboBox.ItemsSourceProperty, new Binding("PageSizeOptions"));
            PageSizeCombobox.SetBinding(ComboBox.SelectedValueProperty, new Binding("PageSize"));
			
            // init doc sections
			DocSectionCombobox.SetBinding(ComboBox.ItemsSourceProperty, new Binding("DocSections"));
			DocSectionCombobox.SetBinding(ComboBox.SelectedValueProperty, new Binding("SelectedDocSection"));
	    }
	}
}



