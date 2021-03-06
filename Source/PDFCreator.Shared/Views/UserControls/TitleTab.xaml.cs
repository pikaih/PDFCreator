﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using pdfforge.PDFCreator.Core.Jobs;
using pdfforge.PDFCreator.Core.Settings;
using pdfforge.PDFCreator.Shared.Helper;
using pdfforge.PDFCreator.Shared.ViewModels.UserControls;

namespace pdfforge.PDFCreator.Shared.Views.UserControls
{
    public partial class TitleTab : UserControl
    {
        private static readonly TranslationHelper TranslationHelper = TranslationHelper.Instance;
        private bool _editingNewRow;

        public TitleTab()
        {
            InitializeComponent();
            if (TranslationHelper.IsInitialized)
            {
                TranslationHelper.TranslatorInstance.Translate(this);
            }

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.TitleReplacements))
            {
                TitleSample_OnTextChanged(null, null);
            }
        }

        private void TitleReplacements_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            TitleSample_OnTextChanged(null, null);
        }

        public TitleTabViewModel ViewModel
        {
            get { return (TitleTabViewModel) DataContext; }
        }

        private void DataGrid_OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Cancel = true;
        }

        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            DataGrid.Dispatcher.BeginInvoke(new Action(EditNewDataGridCell));
        }

        private string ComposeDefaultText()
        {
            return "<" + DataGrid.Columns[0].Header + ">";
        }

        private void EditNewDataGridCell()
        {
            DataGrid.Focus();
            DataGrid.CurrentColumn = DataGrid.Columns[0];
            DataGrid.ScrollIntoView(DataGrid.SelectedItem, DataGrid.Columns[0]);

            DataGridCell cell = GetCell(DataGrid, DataGrid.Items.Count - 1, 1);
            if (cell != null)
            {
                cell.Focus();
                DataGrid.BeginEdit();
                _editingNewRow = true;

                // Setting Data to Grid Cell
                if (cell.Content is TextBox)
                {
                    TextBox textBox = ((TextBox)(cell.Content));
                    textBox.Text = ComposeDefaultText();
                    textBox.SelectAll();
                }
            }
        }

        private DataGridCell GetCell(DataGrid dataGrid, int row, int column)
        {
            DataGridRow rowContainer = GetRow(dataGrid, row);

            if (rowContainer != null)
            {
                DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(rowContainer);

                // try to get the cell but it may possibly be virtualized
                DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                if (cell == null)
                {
                    // now try to bring into view and retreive the cell
                    dataGrid.ScrollIntoView(rowContainer, dataGrid.Columns[column]);
                    cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                }
                return cell;
            }
            return null;
        }

        private DataGridRow GetRow(DataGrid dataGrid, int index)
        {
            DataGridRow row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(index);
            if (row == null)
            {
                // may be virtualized, bring into view and try again
                dataGrid.ScrollIntoView(dataGrid.Items[index]);
                row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(index);
            }
            return row;
        }

        private T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }

        private void DataGrid_OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                if ((_editingNewRow) && (((TextBox) e.EditingElement).Text == ComposeDefaultText()))
                {
                    var titleReplacement = ViewModel.TitleReplacements.First(x => x.Search == ComposeDefaultText());
                    ViewModel.TitleReplacements.Remove(titleReplacement);
                }
            }
            finally
            {
                _editingNewRow = false;
            }
        }

        private void DataGrid_OnRowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (DataGrid.SelectedItem == null)
                return;

            // this is required to fix sorting issues with duplicate elements.
            // When editing is ending, we force a commit, which allows us to call a refresh afterwards
            // Otherwise, we would ask for a refresh while editing

            DataGrid.RowEditEnding -= DataGrid_OnRowEditEnding;
            DataGrid.CommitEdit();
            DataGrid.Items.Refresh();
            DataGrid.RowEditEnding += DataGrid_OnRowEditEnding;

            ViewModel.TitleReplacementView.Refresh();
        }

        private void TitleSample_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (TitlePreview == null || ViewModel.TitleReplacements == null)
                return;

            TitleReplacer titleReplacer = new TitleReplacer();
            titleReplacer.AddReplacements(ViewModel.TitleReplacements);
            TitlePreview.Text = titleReplacer.Replace(TitleSample.Text);
        }
    }
}
