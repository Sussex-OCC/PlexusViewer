using System;
using System.Globalization;

namespace Sussex.Lhcra.Plexus.Viewer.UI.Extensions
{
    public static class AgeHelper
    {
        private const double DaysInYear = 365.242199;
        public static int CalculateAge(this string dateOfBirth)
        {
            try
            {
                var parsedDob = dateOfBirth.ParseDob();
                if(!parsedDob.isValid)
                    throw new ArgumentException("Date of birth given is not a valid UK date format", dateOfBirth);

                var ageDays = (DateTime.Now - parsedDob.dob).TotalDays;
                var ageYrs = (int)(ageDays / DaysInYear);
                return ageYrs;
            }
            catch (Exception e)
            {
                throw new ArgumentException(@"Unable to calculate age for dob: {dateOfBirth}, exception: ", e);
            }
        }

        public static int CalculateAge(this DateTime dateOfBirth)
        {
            try
            { 
                var ageDays = (DateTime.Now - dateOfBirth).TotalDays;
                var ageYrs = (int)(ageDays / DaysInYear);
                return ageYrs;
            }
            catch (Exception e)
            {
                throw new ArgumentException(@"Unable to calculate age for dob: {dateOfBirth}, exception: ", e);
            }
        }

        public static (double ageInDays, int ageInYears) AgeInDaysAndYears(this DateTime dateOfBirth)
        {
            var ageDays = (DateTime.Now - dateOfBirth).TotalDays;
            var ageYrs = (int)(ageDays / DaysInYear);
            return (ageDays, ageYrs);
        }

        public static (bool isValid, DateTime dob) ParseDob(this string dateOfBirth)
        {
            var isValid = DateTime.TryParseExact(dateOfBirth, "dd-MM-yyyy", CultureInfo.GetCultureInfo("en-GB"), DateTimeStyles.None, out var parsedDob);
            return (isValid, parsedDob);
        }
    }
}
