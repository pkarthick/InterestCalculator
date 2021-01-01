using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.IO;
using System.Globalization;
using System.ComponentModel;

using Utilities;
using MahApps.Metro.Controls;


namespace InterestCalculator
{

    public class CustomDataGrid : DataGrid
    {

        private bool justCommitted = false;

        static CustomDataGrid()
        {
            CommandManager.RegisterClassCommandBinding(
                typeof(CustomDataGrid),
                new CommandBinding(ApplicationCommands.Paste,
                    new ExecutedRoutedEventHandler(OnExecutedPaste),
                    new CanExecuteRoutedEventHandler(OnCanExecutePaste)));
        }
        

        protected override void OnCurrentCellChanged(EventArgs e)
        {
            if (justCommitted)
            {

                justCommitted = false;

                object itemToSelect = CurrentItem;

                if (this.CurrentCell.Column != null && (this.CurrentCell.Column.Header.ToString() == "Interest Rate" || this.CurrentCell.Column.Header.ToString() == "Transaction Amount"))
                {
                    //first focus the grid
                    Focus();
                    //then create a new cell info, with the item we wish to edit and the column number of the cell we want in edit mode
                    DataGridCellInfo cellInfo = new DataGridCellInfo(itemToSelect, Columns[0]);
                    //set the cell to be the active one
                    CurrentCell = cellInfo;
                    //scroll the item into view
                    ScrollIntoView(itemToSelect);
                    //begin the edit
                    BeginEdit();
                }

            }

            

            base.OnCurrentCellChanged(e);
        }
        

        #region Clipboard Paste

        private static void OnCanExecutePaste(object target, CanExecuteRoutedEventArgs args)
        {
            ((CustomDataGrid)target).OnCanExecutePaste(args);
        }

        protected override void OnRowEditEnding(DataGridRowEditEndingEventArgs e)
        {
            base.OnRowEditEnding(e);
            justCommitted = (e.EditAction == DataGridEditAction.Commit);
        }


        /// <summary>
        /// This virtual method is called when ApplicationCommands.Paste command query its state.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCanExecutePaste(CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = CurrentCell != null;
            args.Handled = true;
        }

        private static void OnExecutedPaste(object target, ExecutedRoutedEventArgs args)
        {
            ((CustomDataGrid)target).OnExecutedPaste(args);
        }

        /// <summary>
        /// This virtual method is called when ApplicationCommands.Paste command is executed.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnExecutedPaste(ExecutedRoutedEventArgs args)
        {
            this.Paste();
        }

        public void Paste()
        {
            // parse the clipboard data            
            List<string[]> rowData = ClipboardHelper.ParseClipboardData();
            if (rowData != null)
            {
                bool hasAddedNewRow = false;

                // call OnPastingCellClipboardContent for each cell
                int minRowIndex = Items.IndexOf(CurrentItem);
                int maxRowIndex = Items.Count - 1;
                int minColumnDisplayIndex = 0;
                int maxColumnDisplayIndex = Columns.Count - 1;
                int rowDataIndex = 0;
                for (int i = minRowIndex; i <= maxRowIndex && rowDataIndex < rowData.Count; i++, rowDataIndex++)
                {
                    CurrentItem = Items[i];

                    BeginEditCommand.Execute(null, this);

                    int columnDataIndex = 0;
                    for (int j = minColumnDisplayIndex; j <= maxColumnDisplayIndex && columnDataIndex < rowData[rowDataIndex].Length; j++, columnDataIndex++)
                    {
                        DataGridColumn column = ColumnFromDisplayIndex(j);
                        column.OnPastingCellClipboardContent(Items[i], rowData[rowDataIndex][columnDataIndex]);
                    }

                    CommitEditCommand.Execute(this, this);
                    if (i == maxRowIndex)
                    {
                        maxRowIndex++;
                        hasAddedNewRow = true;
                    }
                }

                // update selection
                if (hasAddedNewRow)
                {
                    UnselectAll();
                    UnselectAllCells();

                    CurrentItem = Items[minRowIndex];

                    if (SelectionUnit == DataGridSelectionUnit.FullRow)
                    {
                        SelectedItem = Items[minRowIndex];
                    }
                    else if (SelectionUnit == DataGridSelectionUnit.CellOrRowHeader ||
                             SelectionUnit == DataGridSelectionUnit.Cell)
                    {
                        SelectedCells.Add(new DataGridCellInfo(Items[minRowIndex], Columns[minColumnDisplayIndex]));

                    }
                }
            }
        }

        /// <summary>
        ///     Whether the end-user can add new rows to the ItemsSource.
        /// </summary>
        public bool CanUserPasteToNewRows
        {
            get { return (bool)GetValue(CanUserPasteToNewRowsProperty); }
            set { SetValue(CanUserPasteToNewRowsProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for CanUserAddRows.
        /// </summary>
        public static readonly DependencyProperty CanUserPasteToNewRowsProperty =
            DependencyProperty.Register("CanUserPasteToNewRows",
                                        typeof(bool), typeof(CustomDataGrid),
                                        new FrameworkPropertyMetadata(true, null, null));

        #endregion Clipboard Paste
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {

        int[] daysOfMonth = { 0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        private ObservableCollection<InterestInfo> interestInfoListQ = new ObservableCollection<InterestInfo>();
        private ObservableCollection<InterestInfo> interestInfoListM = new ObservableCollection<InterestInfo>();
        private ObservableCollection<InterestInfo> interestInfoListH = new ObservableCollection<InterestInfo>();
        private ObservableCollection<InterestInfo> interestInfoListA = new ObservableCollection<InterestInfo>();

        private InterestCalculationInput input = new InterestCalculationInput();
        
        

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.InterestInfoListQ.ItemsSource = interestInfoListQ;
            this.InterestInfoListM.ItemsSource = interestInfoListM;
            this.InterestInfoListA.ItemsSource = interestInfoListA;
            this.InterestInfoListH.ItemsSource = interestInfoListH;

            this.InterestRatesGrid.DataContext = input.InterestChangeList;

            this.TransactionGrid.DataContext = input.TransactionList;

            //this.StartDate.Text = "1.5.2012";
            //this.EndDate.Text = "30.11.2014";
            //this.InterestRate.Text = "14.25";
            //this.OutstandingAmount.Text = "195985";

            //input.InterestChangeList.Add(new InterestRateChange { StartDate = new DateTime(2012, 9, 27), InterestRate = 14.00 });
            //input.InterestChangeList.Add(new InterestRateChange { StartDate = new DateTime(2013, 2, 4), InterestRate = 13.95 });
            //input.InterestChangeList.Add(new InterestRateChange { StartDate = new DateTime(2013, 9, 19), InterestRate = 14.05 });
            //input.InterestChangeList.Add(new InterestRateChange { StartDate = new DateTime(2013, 11, 7), InterestRate = 14.25 });


            //input.TransactionList.Add(new TransactionInfo { TransactionDate = new DateTime(2012, 5, 30), TransactionAmount = 4000, IsCredit = true });
            //input.TransactionList.Add(new TransactionInfo { TransactionDate = new DateTime(2013, 3, 18), TransactionAmount = 5000, IsCredit = true });
            //input.TransactionList.Add(new TransactionInfo { TransactionDate = new DateTime(2013, 10, 1), TransactionAmount = 5000, IsCredit = true });
            //input.TransactionList.Add(new TransactionInfo { TransactionDate = new DateTime(2013, 11, 4), TransactionAmount = 5000, IsCredit = true });

            //input.TransactionList.Add(new TransactionInfo { TransactionDate = new DateTime(2013, 3, 21), TransactionAmount = 55, IsCredit = false });

            input.InterestChangeList.CollectionChanged +=InterestChangeList_CollectionChanged;

            input.TransactionList.CollectionChanged += TransactionList_CollectionChanged;

        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //Size s = this.GetNativePrimaryScreenSize();
            TransactionGrid.MaxHeight = InterestRatesGrid.MaxHeight =(this.Height - 350) / 2;
        }

        void TransactionList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Button_Click(this, null);
        }

        void InterestChangeList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Button_Click(this, null);
        }

        private string GetInterestSummary()
        {
            // Gets a NumberFormatInfo associated with the en-US culture.
            NumberFormatInfo nfi = new CultureInfo("en-IN", false).NumberFormat;
            nfi.NumberDecimalDigits = 2;
            DateTime startDate = DateTime.Parse(this.StartDate.Text);

            DateTime endDate = DateTime.Parse(this.EndDate.Text);

            float interestRate = Single.Parse(this.InterestRate.Text);
                        
            double principal = double.Parse(this.OutstandingAmount.Text);

            string interestType = string.Empty;
            bool? isCompound = SimpleInterest.IsSelected == false;

            if (isCompound.HasValue && isCompound.Value)
            {
                

                bool? isMonthly = CompoundInterestTabControl.SelectedIndex == 0;
                bool? isQuarterly = CompoundInterestTabControl.SelectedIndex == 1;
                bool? isHalfYearly = CompoundInterestTabControl.SelectedIndex == 2;
                bool? isAnnual = CompoundInterestTabControl.SelectedIndex == 3;

                if (isMonthly.HasValue && isMonthly.Value)
                {
                    interestType = "monthly compound rests";
                }
                else if (isQuarterly.HasValue && isQuarterly.Value)
                {
                    interestType = "quarterly compound rests";
                }
                else if (isHalfYearly.HasValue && isHalfYearly.Value)
                {
                    interestType = "half yearly compound rests";
                }
                else if (isAnnual.HasValue && isAnnual.Value)
                {
                    interestType = "yearly compound rests";
                }
                else
                {
                    interestType = "compound rests";
                }
                bool hasMultipleInterestRates = input.InterestChangeList.Count > 0;
                if (hasMultipleInterestRates)
                {
                    return string.Format("Interest on {0} with {1} from {2} to {3} for the following rate of interest", principal.ToString("C"), interestType, startDate.ToString("dd.MM.yyyy"), endDate.ToString("dd.MM.yyyy"));
                }
                else
                {
                    return string.Format("Interest on {0} at {1} % p.a. with {2} for the period from {3} to {4}", principal.ToString("C"), interestRate.ToString("N", nfi), interestType, startDate.ToString("dd.MM.yyyy"), endDate.ToString("dd.MM.yyyy"));
                }
            }
            else
            {
                return string.Format("Interest on {0} at {1} % p.a. for the period from {2} to {3}", principal.ToString("C"), interestRate.ToString("N", nfi), startDate.ToString("dd.MM.yyyy"), endDate.ToString("dd.MM.yyyy"));
            }
        }

        private void SetClipboardWithRTF()
        {

            DateTime startDate;
            DateTime.TryParse(this.StartDate.Text, out startDate);

            DateTime endDate;
            DateTime.TryParse(this.EndDate.Text, out endDate);

            double outstandingAmount;
            double.TryParse(this.OutstandingAmount.Text, out outstandingAmount);

            /*
             Interest on Rs.4,540.00 at  24% p. a from 1.3.2004 to 19.1.2007
2 years	:	Rs.2,179.00
10 months	:	Rs.907.00
18 days	:	Rs.53.00
Total	:	Rs.3,139.00

             */

            // Gets a NumberFormatInfo associated with the en-US culture.
            NumberFormatInfo nfi = new CultureInfo("en-IN", false).NumberFormat;
            nfi.NumberDecimalDigits = 2;

            IEnumerable<InterestInfo> interestInfoList = interestInfoListM;

            if (CompoundInterestTabControl.SelectedIndex == 1)
                interestInfoList = interestInfoListQ;
            else if (CompoundInterestTabControl.SelectedIndex == 2)
                interestInfoList = interestInfoListH;
            else if (CompoundInterestTabControl.SelectedIndex == 3)
                interestInfoList = interestInfoListA;


            if (interestInfoList.Any())
            {

                

                RichTextBox doc = new RichTextBox() { FontFamily = new FontFamily("Times New Roman"), FontSize = 15 };
                Table t = new Table() { FontFamily = new FontFamily("Times New Roman"), FontSize = 12, BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0.25), CellSpacing = 10.0 };
                
                TableRowGroup trg = new TableRowGroup();

                if (doc != null)
                {
                    if (this.SimpleInterest.IsSelected)
                    {

                        t.Columns.Add(new TableColumn() { Width = new GridLength(200) });
                        t.Columns.Add(new TableColumn() { Width = new GridLength(125) });
                        t.Columns.Add(new TableColumn() { Width = new GridLength(200) });

                        TableRow tr = new TableRow { };


                        TableCell cell = new TableCell() { };
                        cell.Blocks.Add(new Paragraph(new Run(GetInterestSummary())) { TextAlignment = TextAlignment.Left });
                        cell.ColumnSpan = 3;
                        tr.Cells.Add(cell);

                        trg.Rows.Add(tr);

                        tr = new TableRow();


                        cell = new TableCell();
                        cell.Blocks.Add(new Paragraph(new Run(this.Years.Text + " : ")) { Margin = new Thickness(2, 0, 5, 0), TextAlignment = TextAlignment.Right });
                        tr.Cells.Add(cell);

                        cell = new TableCell();
                        cell.Blocks.Add(new Paragraph(new Run(this.YearsInterest.Text)) { Margin = new Thickness(2, 0, 5, 0), TextAlignment = TextAlignment.Right });
                        tr.Cells.Add(cell);

                        cell = new TableCell();
                        cell.Blocks.Add(new Paragraph(new Run("")) { TextAlignment = TextAlignment.Right });
                        tr.Cells.Add(cell);

                        trg.Rows.Add(tr);

                        tr = new TableRow();

                        cell = new TableCell();
                        cell.Blocks.Add(new Paragraph(new Run(this.Months.Text + " : ")) { Margin = new Thickness(2, 0, 5, 0), TextAlignment = TextAlignment.Right });
                        tr.Cells.Add(cell);

                        cell = new TableCell();
                        cell.Blocks.Add(new Paragraph(new Run(this.MonthsInterest.Text)) { Margin = new Thickness(2, 0, 5, 0), TextAlignment = TextAlignment.Right });
                        tr.Cells.Add(cell);

                        cell = new TableCell();
                        cell.Blocks.Add(new Paragraph(new Run("")) { TextAlignment = TextAlignment.Right });
                        tr.Cells.Add(cell);

                        trg.Rows.Add(tr);


                        tr = new TableRow();

                        cell = new TableCell();
                        cell.Blocks.Add(new Paragraph(new Run(this.Days.Text + " : ")) { Margin = new Thickness(2, 0, 5, 0), TextAlignment = TextAlignment.Right });
                        tr.Cells.Add(cell);

                        cell = new TableCell();
                        cell.Blocks.Add(new Paragraph(new Run(this.DaysInterest.Text)) { Margin = new Thickness(2, 0, 5, 0), TextAlignment = TextAlignment.Right });
                        tr.Cells.Add(cell);

                        cell = new TableCell();
                        cell.Blocks.Add(new Paragraph(new Run("")) { TextAlignment = TextAlignment.Right });
                        tr.Cells.Add(cell);

                        trg.Rows.Add(tr);

                        tr = new TableRow();

                        cell = new TableCell();
                        cell.Blocks.Add(new Paragraph(new Run("Total Interest : ")) { Margin = new Thickness(2, 0, 5, 0), FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right });
                        tr.Cells.Add(cell);

                        cell = new TableCell();
                        //cell.BorderBrush = Brushes.Black;
                        //cell.BorderThickness = new Thickness(0,1,0,1);
                        cell.Blocks.Add(new Paragraph(new Run(this.TotalInterest.Text)) { Margin = new Thickness(2, 0, 5, 0), FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right });
                        tr.Cells.Add(cell);

                        cell = new TableCell();
                        cell.Blocks.Add(new Paragraph(new Run("")) { TextAlignment = TextAlignment.Right });
                        tr.Cells.Add(cell);

                        trg.Rows.Add(tr);

                        tr = new TableRow();

                        cell = new TableCell();

                        cell.Blocks.Add(new Paragraph(new Run("")));
                        cell.ColumnSpan = 3;
                        tr.Cells.Add(cell);

                        trg.Rows.Add(tr);

                        tr = new TableRow();


                        cell = new TableCell();
                        cell.Blocks.Add(new Paragraph(new Run("")) { TextAlignment = TextAlignment.Right });
                        tr.Cells.Add(cell);

                        cell = new TableCell();

                        cell.Blocks.Add(new Paragraph(new Run("Principal : ")) { Margin = new Thickness(2, 0, 5, 0), TextAlignment = TextAlignment.Right });
                        tr.Cells.Add(cell);

                        cell = new TableCell();
                        cell.Blocks.Add(new Paragraph(new Run(double.Parse(this.OutstandingAmount.Text).ToString("C"))) { Margin = new Thickness(2, 0, 5, 0), TextAlignment = TextAlignment.Right });
                        tr.Cells.Add(cell);

                        trg.Rows.Add(tr);


                        tr = new TableRow();

                        cell = new TableCell();
                        cell.Blocks.Add(new Paragraph(new Run("")) { TextAlignment = TextAlignment.Right });
                        tr.Cells.Add(cell);


                        cell = new TableCell();

                        cell.Blocks.Add(new Paragraph(new Run(string.Format("Total Interest : "))) { Margin = new Thickness(2, 0, 5, 0), TextAlignment = TextAlignment.Right });
                        tr.Cells.Add(cell);

                        cell = new TableCell();
                        cell.Blocks.Add(new Paragraph(new Run(this.TotalInterest.Text)) { Margin = new Thickness(2, 0, 5, 0), TextAlignment = TextAlignment.Right });
                        tr.Cells.Add(cell);

                        trg.Rows.Add(tr);


                        tr = new TableRow();

                        cell = new TableCell();
                        cell.Blocks.Add(new Paragraph(new Run("")) { TextAlignment = TextAlignment.Right });
                        tr.Cells.Add(cell);


                        cell = new TableCell();

                        cell.Blocks.Add(new Paragraph(new Run("Total Amount : ") { FontWeight = FontWeights.Bold }){ Margin = new Thickness(2, 0, 5, 0), TextAlignment = TextAlignment.Right });
                        tr.Cells.Add(cell);

                        cell = new TableCell();
                        cell.Blocks.Add(new Paragraph(new Run(this.TotalAmountWithInterest.Text)) { Margin = new Thickness(2, 0, 5, 0), FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right });
                        tr.Cells.Add(cell);

                        trg.Rows.Add(tr);




                        //this.TotalInterest.Text = totalinterest.ToString("C");
                        //this.TotalAmountWithInterest.Text = (principal + totalinterest).ToString("C");

                    }
                    else 
                    {

                        bool hasMultipleInterestRates = input.InterestChangeList.Count > 0;

                        // Create an underline text decoration. Default is underline.
                        TextDecoration myUnderline = new TextDecoration();

                        // Create a linear gradient pen for the text decoration.
                        Pen myPen = new Pen();
                        myPen.Brush = Brushes.Black;
                        myPen.Thickness = 5;
                        myPen.DashStyle = DashStyles.Solid;
                        myUnderline.Pen = myPen;
                        myUnderline.PenThicknessUnit = TextDecorationUnit.FontRecommended;

                        // Set the underline decoration to a TextDecorationCollection and add it to the text block.
                        TextDecorationCollection myCollection = new TextDecorationCollection();
                        myCollection.Add(myUnderline);

                        t.Columns.Add(new TableColumn() { Width = new GridLength(80) }); //Date

                        t.Columns.Add(new TableColumn() { Width = new GridLength(120) }); //IR Period
                        t.Columns.Add(new TableColumn() { Width = new GridLength(40) }); //IR Days
                        
                        if (hasMultipleInterestRates)
                            t.Columns.Add(new TableColumn() { Width = new GridLength(45) }); //IR ROI

                        t.Columns.Add(new TableColumn() { Width = new GridLength(90) }); //Principal

                        t.Columns.Add(new TableColumn() { Width = new GridLength(70) }); //IR Amount

                        t.Columns.Add(new TableColumn() { Width = new GridLength(75) }); //Credit
                        t.Columns.Add(new TableColumn() { Width = new GridLength(65) }); //Debit

                        t.Columns.Add(new TableColumn() { Width = new GridLength(110) });

                        TableRow tr = new TableRow { };

                        TableCell cell = new TableCell() { };

                        tr = new TableRow();

                        cell = new TableCell() { }; //cell.BorderBrush = Brushes.Black;
                     //   cell.BorderThickness = new Thickness(0, 0, 0, 1);
                        cell.Blocks.Add(new Paragraph(new Run("Date")) { Margin = new Thickness(0, 3, 0, 0), TextDecorations = myCollection, TextAlignment = TextAlignment.Center });
                        tr.Cells.Add(cell);


                        cell = new TableCell() { }; //cell.BorderBrush = Brushes.Black;
                        //cell.BorderThickness = new Thickness(0, 0, 0, 1);
                        cell.Blocks.Add(new Paragraph(new Run("Period")) { Margin = new Thickness(0, 3, 0, 0), TextDecorations = myCollection, TextAlignment = TextAlignment.Center });
                        tr.Cells.Add(cell);

                        cell = new TableCell() { }; //cell.BorderBrush = Brushes.Black;
                        //cell.BorderThickness = new Thickness(0, 0, 0, 1);
                        cell.Blocks.Add(new Paragraph(new Run("Days")) { Margin = new Thickness(0, 3, 0, 0), TextDecorations = myCollection, TextAlignment = TextAlignment.Center });
                        tr.Cells.Add(cell);

                        if (hasMultipleInterestRates)
                        {
                            cell = new TableCell(); //cell.BorderBrush = Brushes.Black;
                            //cell.BorderThickness = new Thickness(0, 0, 0, 1);
                            cell.Blocks.Add(new Paragraph(new Run("ROI")) { Margin = new Thickness(0, 3, 0, 0), TextDecorations = myCollection, TextAlignment = TextAlignment.Center });
                            tr.Cells.Add(cell);
                        }

                        cell = new TableCell() { };// cell.BorderBrush = Brushes.Black;
                                                   //     cell.BorderThickness = new Thickness(0, 0, 0, 1);
                        cell.Blocks.Add(new Paragraph(new Run("Principal")) { Margin = new Thickness(0, 3, 0, 0), TextDecorations = myCollection, TextAlignment = TextAlignment.Center });
                        cell.Blocks.Add(new Paragraph(new Run("in Rs.")) { TextDecorations = myCollection, TextAlignment = TextAlignment.Center });
                        tr.Cells.Add(cell);

                        cell = new TableCell() { }; //cell.BorderBrush = Brushes.Black;
                        //cell.BorderThickness = new Thickness(0, 0, 0, 1);
                        cell.Blocks.Add(new Paragraph(new Run("Interest")) { Margin = new Thickness(0, 3, 0, 0), TextDecorations = myCollection, TextAlignment = TextAlignment.Center });
                        cell.Blocks.Add(new Paragraph(new Run("in Rs.")) { TextDecorations = myCollection, TextAlignment = TextAlignment.Center });
                        cell.Blocks.Add(new Paragraph(new Run("(+)")) { TextDecorations = myCollection, TextAlignment = TextAlignment.Center });
                        tr.Cells.Add(cell);
                        cell = new TableCell() { }; //cell.BorderBrush = Brushes.Black;
                                                                //           cell.BorderThickness = new Thickness(0, 0, 0, 1);
                        cell.Blocks.Add(new Paragraph(new Run("Debit")) { Margin = new Thickness(0, 3, 0, 0), TextDecorations = myCollection, TextAlignment = TextAlignment.Center });
                        cell.Blocks.Add(new Paragraph(new Run("in Rs.")) { TextDecorations = myCollection, TextAlignment = TextAlignment.Center });
                        cell.Blocks.Add(new Paragraph(new Run("(+)")) { TextDecorations = myCollection, TextAlignment = TextAlignment.Center });
                        tr.Cells.Add(cell);

                        cell = new TableCell() { }; //cell.BorderBrush = Brushes.Black;
               //         cell.BorderThickness = new Thickness(0, 0, 0, 1);
                        cell.Blocks.Add(new Paragraph(new Run("Credit")) { Margin = new Thickness(0, 3, 0, 0), TextDecorations = myCollection, TextAlignment = TextAlignment.Center });
                        cell.Blocks.Add(new Paragraph(new Run("in Rs.")) { TextDecorations = myCollection, TextAlignment = TextAlignment.Center });
                        cell.Blocks.Add(new Paragraph(new Run("(-)")) { TextDecorations = myCollection, TextAlignment = TextAlignment.Center });
                        tr.Cells.Add(cell);

                        cell = new TableCell() { }; //cell.BorderBrush = Brushes.Black;
                //        cell.BorderThickness = new Thickness(0, 0, 0, 1);
                        cell.Blocks.Add(new Paragraph(new Run("Total")) { Margin = new Thickness(0, 3, 0, 0), TextDecorations = myCollection, TextAlignment = TextAlignment.Center });
                        cell.Blocks.Add(new Paragraph(new Run("in Rs.")) { TextDecorations = myCollection, TextAlignment = TextAlignment.Center });
                        tr.Cells.Add(cell);

                        trg.Rows.Add(tr);
                        
                        //cell.BorderThickness = new Thickness(0, 0, 0, 0);

                        foreach (InterestInfo info in interestInfoList)
                        {
                            tr = new TableRow();


                            cell = new TableCell();
                            cell.Blocks.Add(new Paragraph(new Run(info.Date)) { TextAlignment = TextAlignment.Center });
                            tr.Cells.Add(cell);

                            if (info.HasTransaction)
                            {
                                cell = new TableCell();
                                cell.ColumnSpan = (hasMultipleInterestRates) ? 3 : 2;
                                cell.Blocks.Add(new Paragraph(new Run()));
                                tr.Cells.Add(cell);

                                cell = new TableCell();
                                cell.Blocks.Add(new Paragraph(new Run(info.DisplayPrincipal.Replace("Rs.", ""))) { Margin = new Thickness(2, 0, 5, 0), TextAlignment = TextAlignment.Right });
                                tr.Cells.Add(cell);

                                cell = new TableCell();
                                cell.Blocks.Add(new Paragraph(new Run()));
                                tr.Cells.Add(cell);

                            }
                            else
                            {

                                cell = new TableCell();
                                cell.Blocks.Add(new Paragraph(new Run(string.Format("{0} - {1}", info.DisplayStartDate, info.DisplayEndDate))) { TextAlignment = TextAlignment.Center });
                                tr.Cells.Add(cell);

                                cell = new TableCell();
                                cell.Blocks.Add(new Paragraph(new Run(info.Days.ToString())) { Margin = new Thickness(2, 0, 5, 0), TextAlignment = TextAlignment.Right });
                                tr.Cells.Add(cell);
                                
                                if (hasMultipleInterestRates)
                                {
                                    cell = new TableCell();

                                    cell.Blocks.Add(new Paragraph(new Run(info.InterestRate.ToString("N", nfi))) { Margin = new Thickness(2, 0, 5, 0), TextAlignment = TextAlignment.Right });
                                    tr.Cells.Add(cell);
                                }


                                cell = new TableCell();
                                cell.Blocks.Add(new Paragraph(new Run(info.DisplayPrincipal.Replace("Rs.", ""))) { Margin = new Thickness(2, 0, 5, 0), TextAlignment = TextAlignment.Right });
                                tr.Cells.Add(cell);

                                cell = new TableCell();
                                cell.Blocks.Add(new Paragraph(new Run(info.DisplayInterest.Replace("Rs.", ""))) { Margin = new Thickness(2, 0, 5, 0), TextAlignment = TextAlignment.Right });
                                tr.Cells.Add(cell);
                                
                            }


                            cell = new TableCell();
                            cell.Blocks.Add(new Paragraph(new Run(info.DebitAmount.Replace("Rs.", ""))) { Margin = new Thickness(2, 0, 5, 0), TextAlignment = TextAlignment.Right });
                            tr.Cells.Add(cell);

                            cell = new TableCell();
                            cell.Blocks.Add(new Paragraph(new Run(info.CreditAmount.Replace("Rs.", ""))) { Margin = new Thickness(2, 0, 5, 0), TextAlignment = TextAlignment.Right });
                            tr.Cells.Add(cell);

                            cell = new TableCell();
                            cell.Blocks.Add(new Paragraph(new Run(info.DisplayTotalWithInterest.Replace("Rs.", ""))) { Margin = new Thickness(2, 0, 5, 0), TextAlignment = TextAlignment.Right });
                            tr.Cells.Add(cell);

                            trg.Rows.Add(tr);

                        }
                        
                        tr = new TableRow();

                        TextDecoration underline = new TextDecoration() { Pen = new Pen(Brushes.Black, 1) { DashStyle = DashStyles.Solid } };
                        cell = new TableCell();

                        cell.Blocks.Add(new Paragraph(new Run("Total Amount : ")) { Margin = new Thickness(2, 0, 5, 0), TextAlignment = TextAlignment.Right });
                        if (hasMultipleInterestRates) { cell.ColumnSpan = 8; } else { cell.ColumnSpan = 7; }
                        tr.Cells.Add(cell);

                        cell = new TableCell() { ColumnSpan = 1 };
                        cell.Blocks.Add(new Paragraph(new Run((interestInfoList.Last().TotalWithInterest).ToString("C"))) { Margin = new Thickness(2, 0, 5, 0), FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right });
                        tr.Cells.Add(cell);

                        trg.Rows.Add(tr);

                    }


                    t.RowGroups.Add(trg);
                    
                    doc.Document.Blocks.Add(new Paragraph(new Run("INTEREST CALCULATION")) { FontWeight = FontWeights.Bold , TextAlignment = TextAlignment.Center });

                    doc.Document.Blocks.Add(new Paragraph(new Run(" ")) { TextAlignment = TextAlignment.Center });

                    Table table = new Table();

                    table.FontFamily = new FontFamily("Times New Roman");
                    table.FontSize = 16;
                    
                    table.BorderBrush = Brushes.LightGray;
                    table.BorderThickness = new Thickness(0.25);

                    table.Columns.Add(new TableColumn() { Width = new GridLength(320) });
                    table.Columns.Add(new TableColumn() { Width = new GridLength(320) });

                    TableRowGroup trg1 = new TableRowGroup();
                    TableRow tr1 = new TableRow();
                    tr1.Cells.Add(new TableCell(new Paragraph(new Run("BORROWER NAME") { FontWeight=FontWeights.Bold })));
                    tr1.Cells.Add(new TableCell(new Paragraph(new Run("ACCOUNT NUMBER") { FontWeight = FontWeights.Bold })));

                    TableRow tr2 = new TableRow();
                    tr2.Cells.Add(new TableCell(new Paragraph(new Run(BorrowerName.Text))));
                    tr2.Cells.Add(new TableCell(new Paragraph(new Run(AccountNumber.Text))));

                    trg1.Rows.Add(tr1);
                    trg1.Rows.Add(tr2);

                    table.RowGroups.Add(trg1);

                    doc.Document.Blocks.Add(table);

                    doc.Document.Blocks.Add(new Paragraph(new Run(" ")) { TextAlignment = TextAlignment.Center });

                    doc.Document.Blocks.Add(new Paragraph(new Run(GetInterestSummary())) { TextAlignment = TextAlignment.Left });

                    doc.Document.Blocks.Add(new Paragraph(new Run(" ")) { TextAlignment = TextAlignment.Center });

                    doc.Document.Blocks.Add(t);

                    TextRange range = new TextRange(doc.Document.ContentStart, doc.Document.ContentEnd);
                    MemoryStream ms = new MemoryStream();
                    range.Save(ms, DataFormats.Rtf);
                    string xamlText = ASCIIEncoding.Default.GetString(ms.ToArray());

                    Clipboard.SetData(DataFormats.Rtf, xamlText);
                }


            }
            else
                Clipboard.SetData(DataFormats.Rtf, string.Empty);

        }

        private bool isLeapYear(double year)
        {
            return (year % 400 == 0 || (year % 100 != 0 && year % 4 == 0));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CalculateInterest();

            }
            catch
            {

            }


        }

        private void CalculateInterest()
        {
            InterestCalculationInput input = GetInput(0);

            this.interestInfoListQ.Clear();
            this.interestInfoListA.Clear();
            this.interestInfoListH.Clear();
            this.interestInfoListM.Clear();

            CalculateSimpleInterest();

            Populate(interestInfoListQ, 1);
            Populate(interestInfoListH, 2);
            Populate(interestInfoListM, 0);
            Populate(interestInfoListA, 3);

            SetClipboardWithRTF();
        }

        private void Populate(ObservableCollection<InterestInfo> interestInfoCollection, int index)
        {
            InterestCalculationInput input = GetInput(index);
            List<InterestInfo> interestList = InterestCalculationManager.CreateInterestDetails(input);

            foreach (InterestInfo info in interestList)
                interestInfoCollection.Add(info);
        }

        private void CalculateSimpleInterest()
        {
            InterestCalculationInput input = GetInput(0);
            DateTime startDate = input.StartDate;
            DateTime endDate = input.EndDate;


            TimeSpan t = input.EndDate - input.StartDate;

            int totalDays = Convert.ToInt32(t.TotalDays) + 1;

            int yeardiff = totalDays / 365;

            int monthdiff = (totalDays % 365) / 30;

            DateTime afterYear = input.StartDate.AddYears(yeardiff);

            DateTime afterMonths = afterYear.AddMonths(monthdiff);

            int daydiff = Convert.ToInt32((input.EndDate - afterMonths).TotalDays) + 1;

            DateTime afterDays = afterMonths.AddDays(daydiff);


            //int daydiff = endDate.Day - startDate.Day;

            //if (daydiff < 0)
            //{
            //    daydiff += (startDate.Month == 2 && isLeapYear(startDate.Year)) ? 29 : daysOfMonth[startDate.Month];
            //    endDate = endDate.AddMonths(-1);
            //}

            //int monthdiff = endDate.Month - startDate.Month;
            //if (monthdiff < 0)
            //{
            //    monthdiff += 12;
            //    endDate = endDate.AddYears(-1);
            //}

            //int yeardiff = endDate.Year - startDate.Year;

            //daydiff++;

            //if (daydiff == 31)
            //{
            //    daydiff = 0;
            //    monthdiff++;
            //}

            //if (monthdiff == 12)
            //{
            //    monthdiff = 0;
            //    yeardiff++;
            //}

            double interestamount = input.Principal * input.InterestRate / 100;
            
            double yearsinterest = (yeardiff * interestamount);
            double monthsinterest = monthdiff * interestamount / 12;
            double daysinterest = daydiff * interestamount / 365;

            double xdaysinterest = totalDays * interestamount / 365;
            double totalinterest = yearsinterest + monthsinterest + daysinterest;


            this.Years.Text = yeardiff.ToString() + (yeardiff == 1 ? " year" : " years");
            this.Months.Text = monthdiff.ToString() + (monthdiff == 1 ? " month" : " months");
            this.Days.Text = (daydiff).ToString() + (daydiff == 1 ? " day" : " days"); ;

            this.XDays.Text = totalDays.ToString() + " days";

            this.YearsInterest.Text = yearsinterest.ToString("C");
            this.MonthsInterest.Text = monthsinterest.ToString("C");
            this.DaysInterest.Text = daysinterest.ToString("C");

            this.XDaysInterest.Text = xdaysinterest.ToString("C");

            this.TotalInterest.Text = totalinterest.ToString("C");
            this.XTotalInterest.Text = xdaysinterest.ToString("C");

            this.TotalAmountWithInterest.Text = (input.Principal + totalinterest).ToString("C");
            this.XTotalAmountWithInterest.Text = (input.Principal + xdaysinterest).ToString("C");

        }

        private InterestCalculationInput GetInput(int index)
        {
            
            input.IsSimple = this.SimpleInterest.IsSelected;
            input.IsCompound = !this.SimpleInterest.IsSelected;
                        
            input.IsMonthly = index == 0;
            input.IsQuarterly = index == 1;
            input.IsHalfYearly = index == 2;
            input.IsAnnual = index == 3;

            input.StartDate = DateTime.Parse(this.StartDate.Text);

            input.EndDate = DateTime.Parse(this.EndDate.Text);
            
            input.InterestRate = Math.Round(Single.Parse(this.InterestRate.Text), 2);

            input.Principal = double.Parse(this.OutstandingAmount.Text);

            return input;

        }
        
        
        private void CompoundInterestTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetClipboardWithRTF();
        }
    }



}
