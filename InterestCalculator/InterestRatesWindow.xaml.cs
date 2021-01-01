using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Utilities;

namespace InterestCalculator
{
    /// <summary>
    /// Interaction logic for InterestRatesWindow.xaml
    /// </summary>
    public partial class InterestRatesWindow : Window
    {
        public InterestRatesWindow()
        {
            InitializeComponent();
            this.Closing += new System.ComponentModel.CancelEventHandler(InterestRatesWindow_Closing);
        }

        void InterestRatesWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
           
          ObservableCollection<InterestRateChange> list = this.DataContext as ObservableCollection<InterestRateChange>;

           if(list != null) {
             

             var groups = list.GroupBy(irc => irc.StartDate).ToList();

             if( groups != null && groups.Count > 0)
             {
               if(groups.Any(g => g.Count() > 1)) {
                 MessageBox.Show("Duplicate dates found. Please correct."); 
                 e.Cancel = true;
               }
             }

           }
        }

        private void DataGrid_LoadingRowDetails_1(object sender, DataGridRowDetailsEventArgs e)
        {
            
        }

        private void DataGrid_LoadingRow_1(object sender, DataGridRowEventArgs e)
        {
            InterestRateChange interestRateChange = e.Row.Item as InterestRateChange;

            if (interestRateChange != null)
            {
                ObservableCollection<InterestRateChange> list = this.DataContext as ObservableCollection<InterestRateChange>;

                if (list != null)
                {
                    int index = list.IndexOf(interestRateChange);
                    if (index >= 1)
                    {
                        interestRateChange.StartDate = list[index - 1].StartDate.AddDays(1);
                    }
                }
            }

        }

    }
}
