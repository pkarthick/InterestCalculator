using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Utilities
{
    public static class InterestCalculationManager
    {
        static int[] daysOfMonth = { 0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        
        private static DateTime GetTempEndDate(InterestCalculationInput input)
        {

            DateTime startDate = input.StartDate;

            DateTime tempEndDate = new DateTime(startDate.Year, startDate.Month, daysOfMonth[startDate.Month]);

            if (input.IsMonthly.HasValue && input.IsMonthly.Value)
            {
                if (startDate.Month == 2 && tempEndDate.Year % 4 == 0)
                    tempEndDate = new DateTime(startDate.Year, startDate.Month, 29);
            }
            else if (input.IsQuarterly.HasValue && input.IsQuarterly.Value)
            {
                if (startDate.Month <= 3)
                    tempEndDate = new DateTime(startDate.Year, 3, daysOfMonth[3]);
                else if (startDate.Month <= 6)
                    tempEndDate = new DateTime(startDate.Year, 6, daysOfMonth[6]);
                else if (startDate.Month <= 9)
                    tempEndDate = new DateTime(startDate.Year, 9, daysOfMonth[9]);
                else if (startDate.Month <= 12)
                    tempEndDate = new DateTime(startDate.Year, 12, daysOfMonth[12]);
            }
            else if (input.IsHalfYearly.HasValue && input.IsHalfYearly.Value)
            {
                if (startDate.Month <= 3)
                    tempEndDate = new DateTime(startDate.Year, 3, daysOfMonth[3]);
                else if (startDate.Month <= 9)
                    tempEndDate = new DateTime(startDate.Year, 9, daysOfMonth[9]);
            }
            else if (input.IsAnnual.HasValue && input.IsAnnual.Value)
            {
                tempEndDate = new DateTime(startDate.Year, 12, daysOfMonth[12]);
            }
            return tempEndDate;
        }

        public static List<InterestInfo> CreateInterestDetails(InterestCalculationInput input)
        {
            List<InterestInfo> interestInfoList = new List<InterestInfo>();

            DateTime tempEndDate = GetTempEndDate(input);

            DateTime startDate = input.StartDate;

            DateTime tempStartDate = startDate;

            double interestRate = input.InterestRate;
            double principal = input.Principal;

            List<InterestRateChange> interestChangeList = input.InterestChangeList.ToList();
            List<TransactionInfo> transactionList = input.TransactionList.ToList();
            
            InterestInfo info = new InterestInfo { TransactionDate = tempStartDate, HasTransaction = true, TransactionAmount = 0.0, InterestRate=0.0, IsCredit=true, Principal=principal }; ;
            interestInfoList.Add(info);

            do
            {
                if (tempEndDate > input.EndDate)
                {
                    tempEndDate = input.EndDate;
                }

                List<InterestRateChange> changesForTheCurrentPeriod =
                    interestChangeList
                    .Where(irc => irc.StartDate >= tempStartDate && irc.StartDate <= tempEndDate)
                    .OrderBy(irc => irc.StartDate).ToList();

                var datesList = changesForTheCurrentPeriod.Select(irc => new { Date = irc.StartDate, IsTransaction = false }).ToList();

                List<TransactionInfo> transactionsForTheCurrentPeriod =
                           transactionList
                           .Where(txn => txn.TransactionDate >= tempStartDate && txn.TransactionDate <= tempEndDate)
                           .OrderBy(txn => txn.TransactionDate).ToList();

                datesList.AddRange( transactionsForTheCurrentPeriod.Distinct(new TransactionInfoEqualityComparer()).Select(ti=>new { Date=ti.TransactionDate, IsTransaction = true }));
                
                datesList.OrderBy(di=>di.Date).ToList().ForEach(
                    di =>
                    {
                        if (di.IsTransaction)
                        {

                            List<TransactionInfo> transactionOnThisDate = transactionsForTheCurrentPeriod.Where(ti => ti.TransactionDate == di.Date)
                                .ToList();

                            if (transactionOnThisDate.Any())
                            {

                                DateTime transDate = transactionOnThisDate.First().TransactionDate.AddDays(-1);

                                if (tempStartDate == transactionOnThisDate.First().TransactionDate)
                                {
                                    transDate = tempStartDate;
                                }

                                info = new InterestInfo { StartDate = tempStartDate, EndDate = transDate, InterestRate = interestRate, Principal = principal };
                                interestInfoList.Add(info);

                                principal = info.TotalWithInterest;

                                transactionOnThisDate.ForEach(ti
                                    =>
                                    {

                                        info = new InterestInfo { TransactionDate = ti.TransactionDate, Principal = info.TotalWithInterest, HasTransaction = true, IsCredit = ti.IsCredit, TransactionAmount = ti.TransactionAmount };
                                        interestInfoList.Add(info);

                                        principal = info.TotalWithInterest;
                                        tempStartDate = ti.TransactionDate;

                                    });

                                if (tempStartDate == transactionOnThisDate.First().TransactionDate && transactionOnThisDate.First().IsCredit)
                                {
                                    tempStartDate = tempStartDate.AddDays(1);
                                }

                            }
                        }
                        else
                        {

                            changesForTheCurrentPeriod
                                .Where(irc => irc.StartDate  == di.Date)
                                .OrderBy(irc => irc.StartDate)
                                .ToList()
                                .ForEach(
                                irc =>
                                {
                                    if (tempStartDate < irc.StartDate)
                                    {
                                        info = new InterestInfo { StartDate = tempStartDate, EndDate = irc.StartDate.AddDays(-1), InterestRate = interestRate, Principal = principal };
                                        interestInfoList.Add(info);
                                        principal = info.TotalWithInterest;
                                        tempStartDate = irc.StartDate;
                                    }

                                    interestRate = irc.InterestRate;
                                }
                          );
                        }
                    }
                );

                if (tempStartDate <= tempEndDate)
                {
                    info = new InterestInfo { StartDate = tempStartDate, EndDate = tempEndDate, InterestRate = interestRate, Principal = principal };
                    interestInfoList.Add(info);

                    principal = info.TotalWithInterest;

                    tempStartDate = tempEndDate.AddDays(1);
                }


                if (input.IsMonthly.HasValue && input.IsMonthly.Value)
                {
                    tempEndDate = tempEndDate.AddMonths(1);
                    tempEndDate = new DateTime(tempEndDate.Year, tempEndDate.Month, (tempEndDate.Month == 2 && isLeapYear(tempEndDate.Year)) ? 29 : daysOfMonth[tempEndDate.Month]);
                }
                else if (input.IsQuarterly.HasValue && input.IsQuarterly.Value)
                {
                    tempEndDate = tempEndDate.AddMonths(3);
                    tempEndDate = new DateTime(tempEndDate.Year, tempEndDate.Month, (tempEndDate.Month == 2 && isLeapYear(tempEndDate.Year)) ? 29 : daysOfMonth[tempEndDate.Month]);
                }
                else if (input.IsHalfYearly.HasValue && input.IsHalfYearly.Value)
                {
                    tempEndDate = tempEndDate.AddMonths(6);
                    tempEndDate = new DateTime(tempEndDate.Year, tempEndDate.Month, (tempEndDate.Month == 2 && isLeapYear(tempEndDate.Year)) ? 29 : daysOfMonth[tempEndDate.Month]);
                }
                else if (input.IsAnnual.HasValue && input.IsAnnual.Value)
                {
                    tempEndDate = tempEndDate.AddYears(1);
                    tempEndDate = new DateTime(tempEndDate.Year, tempEndDate.Month, (tempEndDate.Month == 2 && isLeapYear(tempEndDate.Year)) ? 29 : daysOfMonth[tempEndDate.Month]);
                }

            }
            while (tempStartDate <= input.EndDate || tempEndDate <= input.EndDate);
            
            //transactionList
            //.Where(txn => txn.IsCredit == false)
            //.OrderBy(txn => txn.TransactionDate)
            //.ToList()
            //.ForEach(txn =>
            //{
            //    info = new InterestInfo { TransactionDate = txn.TransactionDate, Principal = principal, HasTransaction = true, IsCredit = txn.IsCredit, TransactionAmount = txn.TransactionAmount };
            //    interestInfoList.Add(info);
            //    principal += txn.TransactionAmount;
            //}
            //);

            return interestInfoList;

        }

        public static double GetSimpleInterest(InterestCalculationInput input)
        {
            int daydiff, monthdiff, yeardiff;
            GetDurationBreakup(input.StartDate, input.EndDate, out daydiff, out monthdiff, out yeardiff);

            double interestamount = input.Principal * input.InterestRate / 100;

            double yearsinterest = (yeardiff * interestamount);
            double monthsinterest = monthdiff * interestamount / 12;
            double daysinterest = daydiff * interestamount / 366;
            double totalinterest = yearsinterest + monthsinterest + daysinterest;

            return totalinterest;

        }

        public static bool isLeapYear(double year)
        {
            return (year % 400 == 0 || (year % 100 != 0 && year % 4 == 0));
        }

        public static string GetDurationBreakup(DateTime startDate, DateTime endDate)
        {
            int daydiff, monthdiff, yeardiff;
            GetDurationBreakup(startDate, endDate, out daydiff, out monthdiff, out yeardiff);

            Func<int, string, string> durationInfo = (diff, durationType) =>
            {

                if (diff > 1)
                    return $"{diff} {durationType}s, ";
                else if (diff == 1)
                    return $"{diff} {durationType}, ";

                return string.Empty;

            };
            
            return $@"{durationInfo(yeardiff, "year")}{durationInfo(monthdiff, "month")}{durationInfo(daydiff, "day")}".Trim( ',', ' ');
        }

        public static void GetDurationBreakup(DateTime startDate, DateTime endDate, out int daydiff, out int monthdiff, out int yeardiff)
        {
            daydiff = endDate.Day - startDate.Day;
            if (daydiff < 0)
            {
                daydiff += (startDate.Month == 2 && isLeapYear(startDate.Year)) ? 29 : daysOfMonth[startDate.Month];
                endDate = endDate.AddMonths(-1);
            }

            monthdiff = endDate.Month - startDate.Month;
            if (monthdiff < 0)
            {
                monthdiff += 12;
                endDate = endDate.AddYears(-1);
            }

            yeardiff = endDate.Year - startDate.Year;
            daydiff++;

            if (daydiff == 31)
            {
                daydiff = 0;
                monthdiff++;
            }

            if (monthdiff == 12)
            {
                monthdiff = 0;
                yeardiff++;
            }
            
        }

        public static double GetInterest(string interestType, string principal, string startDate, string endDate, string interestRate)
        {
            InterestCalculationInput input = new InterestCalculationInput();

            try
            {

                interestType = interestType.Trim().ToUpper();

                char interestIdentifier = interestType[0];

                switch (interestIdentifier)
                {
                    case 'S':
                        input.IsSimple = true;
                        break;

                    case 'M':
                        input.IsCompound = true;
                        input.IsMonthly = true;
                        break;

                    case 'Q':
                        input.IsCompound = true;
                        input.IsQuarterly = true;
                        break;

                    case 'H':
                        input.IsCompound = true;
                        input.IsHalfYearly = true;
                        break;

                    case 'Y':
                        input.IsCompound = true;
                        input.IsAnnual = true;
                        break;

                        
                }

                input.StartDate = DateTime.Parse(startDate);

                input.EndDate = DateTime.Parse(endDate);

                input.InterestRate = Math.Round(Single.Parse(interestRate), 2);

                input.Principal = double.Parse(principal);

                if (interestIdentifier == 'S')
                {
                    return GetSimpleInterest(input);
                }
                else
                {
                    List<InterestInfo> interestInfoList = CreateInterestDetails(input);
                    return (interestInfoList.Last().TotalWithInterest - input.Principal);
                }

            }
            catch 
            {
                
            }


            return 0.0;
        }

    }
}
