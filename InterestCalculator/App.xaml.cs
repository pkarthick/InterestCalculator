using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Globalization;
using System.Threading;

namespace InterestCalculator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

           

            CultureInfo cultureInfo = (CultureInfo) CultureInfo.GetCultureInfo("en-in").Clone();

            cultureInfo.NumberFormat.CurrencySymbol = "Rs.";

            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;


        }
    }
}
