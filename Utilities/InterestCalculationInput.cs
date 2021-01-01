using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Utilities
{
    public class InterestCalculationInput
    {

        public bool? IsSimple { get; set; }

        public bool? IsCompound { get; set; }

        public bool? IsMonthly { get; set; }

        public bool? IsQuarterly { get; set; }

        public bool? IsHalfYearly { get; set; }

        public bool? IsAnnual { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public double InterestRate { get; set; }

        public double Principal { get; set; }

        public double OutstandingAmount { get; set; }
                
        public ObservableCollection<InterestRateChange> InterestChangeList { get; set; }
        public ObservableCollection<TransactionInfo> TransactionList { get; set; }

        public InterestCalculationInput()
        {
            this.InterestChangeList = new ObservableCollection<InterestRateChange>();
            this.TransactionList = new ObservableCollection<TransactionInfo>();
        }


    }



    public class TransactionInfoEqualityComparer : IEqualityComparer<TransactionInfo>
    {

        public bool Equals(TransactionInfo x, TransactionInfo y)
        {
            return x.TransactionDate == y.TransactionDate;
        }

        public int GetHashCode(TransactionInfo obj)
        {
            return this.GetHashCode();
        }

    }

    public class TransactionInfo
    {

        public bool IsCredit { get; set; }

        public string DisplayTransactionDate
        {
            get { return this.TransactionDate == DateTime.MinValue ? "" : this.TransactionDate.ToString("dd.MM.yyyy"); }
            set
            {

                DateTime temp;

                if (DateTime.TryParse(value, CultureInfo.CurrentUICulture, DateTimeStyles.None, out temp))
                {
                    this.TransactionDate = temp;
                }

            }
        }

        public DateTime TransactionDate
        {
            get;
            set;
        }

        public double TransactionAmount
        {
            get;
            set;
        }


    }

    public class InterestRateChange
    {

        public DateTime StartDate
        {
            get;
            set;
        }

        public string DisplayStartDate
        {
            get { return this.StartDate == DateTime.MinValue ? "" : this.StartDate.ToString("dd.MM.yyyy"); }
            set
            {

                DateTime temp;

                if (DateTime.TryParse(value, CultureInfo.CurrentUICulture, DateTimeStyles.None, out temp))
                {
                    this.StartDate = temp;
                }

            }
        }

        public double InterestRate
        {
            get;
            set;
        }

    }

    public class InterestInfo
    {

        public bool HasTransaction { get; set; }

        public double TransactionAmount { get; set; }

        public bool IsCredit { get; set; }

        public DateTime TransactionDate { get; set; }

        public string Date
        {

            get
            {
                if (HasTransaction)
                    return TransactionDate.ToString("dd.MM.yyyy");
                else
                    return StartDate.ToString("dd.MM.yyyy");
            }

        }

        public string CreditAmount
        {
            get
            {
                if (HasTransaction && IsCredit && TransactionAmount != 0.00)
                    return TransactionAmount.ToString("0.00");
                else
                    return string.Empty;
            }
        }

        public string DebitAmount
        {
            get
            {
                if (HasTransaction && !IsCredit)
                    return TransactionAmount.ToString("0.00");
                else
                    return string.Empty;
            }
        }


        public double Principal
        {
            get;
            set;
        }

        public string DisplayPrincipal
        {
            get
            {

                return Principal.ToString("0.00");
            }
        }

        public string DisplayStartDate
        {
            get { if (HasTransaction) return string.Empty; return this.StartDate.ToString("dd.MM.yy"); }
        }

        public string DisplayEndDate
        {
            get { if (HasTransaction) return string.Empty; return this.EndDate.ToString("dd.MM.yy"); }
        }

        public DateTime StartDate
        {
            get;
            set;
        }

        public DateTime EndDate
        {
            get;
            set;
        }

        public string DisplayDays
        {
            get
            {
                if (HasTransaction)
                    return string.Empty;
                else
                    return Days.ToString();
            }
        }

        public int Days
        {
            get
            {
                return (int)((EndDate - StartDate).TotalDays + 1);
            }
        }

        public double InterestRate
        {
            get;
            set;
        }

        public string DisplayInterestRate
        {
            get
            {
                if (HasTransaction)
                    return string.Empty;

                return this.InterestRate.ToString("0.00");

            }
        }

        public string DisplayTotalWithInterest
        {
            get
            {
                return this.TotalWithInterest.ToString("0.00");
            }
        }

        public double Interest
        {
            get
            {
                if (HasTransaction)
                    return 0.0;

                double daysPerYear = isLeapYear(EndDate.Year) ? 366.0 : 365.0;
                return Math.Round(((Days * 1.0) / daysPerYear) * Principal * InterestRate / 100, 2);
            }
        }

        public string DisplayInterest
        {
            get
            {
                if (HasTransaction)
                    return string.Empty;

                return Interest.ToString("0.00");
            }
        }

        public double TotalWithInterest
        {
            get
            {

                if (HasTransaction)
                    return Principal + TransactionAmount * (IsCredit ? -1 : 1);

                double daysPerYear = isLeapYear(EndDate.Year) ? 366.0 : 365.0;
                return Math.Round(Principal, 2) + Math.Round(Interest, 2);

            }
        }

        private bool isLeapYear(double year)
        {
            return (year % 400 == 0 || (year % 100 != 0 && year % 4 == 0));
        }
    }

}
