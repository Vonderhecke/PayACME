/*
 * ioet exercise
 * Date 10.01.2019
 * Autor Ingo Delahaye
 * 
 */

using System;
using System.Collections.Generic;
using System.IO;

namespace PayACME
{
    class Program
    {
        static readonly uint[,] PayRates = new uint[,] { { 25, 15, 20 }, { 30, 20, 25 } };
        
        //struct PayRate
        //{
        //    TimeSpan from;
        //    TimeSpan to;
        //    uint payVal;

        //    public PayRate(TimeSpan from, TimeSpan to, uint payVal)
        //    {
        //        this.from = from;
        //        this.to = to;
        //        this.payVal = payVal;
        //    }
        //}

        //static  PayRate[] WeekDayPayRate =
        //{
        //    new PayRate(new TimeSpan(0, 1, 0), new TimeSpan(9, 0, 0), 25),
        //    new PayRate(new TimeSpan(9, 1, 0), new TimeSpan(18, 0, 0), 15),
        //    new PayRate(new TimeSpan(18, 1, 0), new TimeSpan(0, 0, 0), 20)
        //};

        //static PayRate[] WeekendPayRate =
        //{
        //    new PayRate(new TimeSpan(0, 1, 0), new TimeSpan(9, 0, 0), 30),
        //    new PayRate(new TimeSpan(9, 1, 0), new TimeSpan(18, 0, 0), 20),
        //    new PayRate(new TimeSpan(18, 1, 0), new TimeSpan(0, 0, 0), 25)
        //}; 


        static void Main(string[] args)
        {
            string[] wHoursFileLines;

            if (args.Length != 1)
            {
                Console.WriteLine("Usage:\nPayACME <filename>");
                return;
            }

            // read input file line-splitted
            try
            {
                wHoursFileLines = File.ReadAllLines(args[0]);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            if (wHoursFileLines.Length == 0)
            {
                Console.WriteLine("No input found in " + args[0]);
                return;
            }

            foreach (string wHoursLine in wHoursFileLines)
            {
                if (wHoursLine == string.Empty) continue;
                if (CalculatePayVal(wHoursLine, out string employee, out double payVal, out string errMsg))
                {
                    Console.WriteLine("The amount to pay " + employee + " is " + payVal.ToString("0.00") + " USD");
                    if (payVal > 9999)
                    {
                        Console.WriteLine("\tThe employee needs a vacation! :-;");
                    }
                }
                else
                {
                    Console.WriteLine(errMsg);
                }
            }

#if DEBUG
            Console.ReadKey();
#endif
        }

        /// <summary>
        /// Calculates the salary of an employee
        /// </summary>
        /// <param name="input"></param>
        /// <param name="employee"></param>
        /// <param name="payVal"></param>
        /// <param name="errMsg"></param>
        /// <returns>true if no errors found</returns>
        static bool CalculatePayVal(string input, out string employee, out double payVal, out string errMsg) 
        {
            errMsg = "Error in line \"" + input + "\":\n\t";
            employee = "unknown";
            payVal = 0;

            if (!input.Contains("="))
            {
                errMsg += "Could not find \"=\".\nPlease seperate the name of the employee with an equal sign from the working hours.";
                return false;
            }

            int equalPos = input.IndexOf('=');
            if (equalPos == 0)
            {
                errMsg += "Missing name of the employee";
                return false;
            }
            
            employee = input.Substring(0, equalPos);

            if (input.Length == equalPos +1)
            {
                // The employee did not work --> PayVal = 0
                return true;
            }

            string[] wHours = input.Substring(equalPos + 1).Split(',');
            if (wHours.Length == 0)
            {
                // The employee did not work --> PayVal = 0
                return true;
            }

            foreach (string wHour in wHours)
            {
                int weekDay = GetWeekDay(wHour);
                if (weekDay == -1)
                {
                    errMsg += "Weekday in " + wHour + " could not be interpreted.";
                    return false;
                }

                if (wHour.Length < 3) continue; // did not work that weekday
                string[] fromTo = wHour.Substring(2).Split('-');
                if (fromTo.Length != 2)
                {
                    errMsg += "Timespan in " + wHour + " could not be interpreted.";
                    return false;
                }

                if (!TimeSpan.TryParseExact(fromTo[0], "hh\\:mm", null, out TimeSpan from))
                {
                    errMsg += "Timespan from in " + wHour + " could not be interpreted.";
                    return false;
                }

                // not necessary because intercepted by TryParseExact(hh:mm):
                /*if (from.TotalHours < 0 || from.TotalHours >= 24)
                {
                    errMsg += "Wrong range in timespan from in " + wHour;
                    return false;
                }*/

                if (!TimeSpan.TryParseExact(fromTo[1], "hh\\:mm", null, out TimeSpan to))
                {
                    errMsg += "Timespan to in " + wHour + " could not be interpreted.";
                    return false;
                }

                // not necessary because intercepted by TryParseExact(hh:mm):
                /*if (to.TotalHours < 0 || to.TotalHours >= 24)
                {
                    errMsg += "Wrong range in timespan to in " + wHour;
                    return false;
                }*/

                payVal += CalculateDayPayVal(weekDay, from, to);
                // check if payVal could get out of range not nessary.

            }

            return true;
        }

        /// <summary>
        /// Calculates the salary of an employee for a Timespan at a given weekday
        /// </summary>
        /// <param name="weekDay">Monday = 0</param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        static double CalculateDayPayVal(int weekDay, TimeSpan from, TimeSpan to)
        {
            double dayVal = 0;
            uint minSum = 0;

            int fromMin = (int)from.TotalMinutes;
            int toMin = (int)to.TotalMinutes;
            int payRateIndex = fromMin / (9 * 60);

            while (fromMin != toMin)
            {
                minSum++;
                fromMin++;
                if (payRateIndex != fromMin / (9 * 60) || fromMin == 24 * 60)
                {
                    dayVal += PayRates[weekDay / 5, payRateIndex] * minSum / 60.0; 
                    minSum = 0;

                    if (fromMin == 24 * 60)
                    {
                        fromMin = 0;
                        weekDay = (weekDay +1) % 7;
                    }
                    payRateIndex = fromMin / (9 * 60);
                }
            }
            dayVal += PayRates[weekDay / 5, payRateIndex] * minSum / 60.0;
            return dayVal;
        }

        /// <summary>
        /// Calculate the Index of PayRates[,] from the week day code
        /// </summary>
        /// <param name="dayHours"></param>
        /// <returns></returns>
        static int GetWeekDay(string dayHours)
        {
            List<string> wDays =  new List<string> { "MO", "TU", "WE", "TH", "FR", "SA", "SU" };
            if (dayHours.Length < 2) return -1;
            string wCode = dayHours.Substring(0, 2);
            return wDays.IndexOf(wCode);
        }


    }
}
