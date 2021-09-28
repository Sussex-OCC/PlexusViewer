using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.UI.Extensions
{
    public static class AgeExtensions
    {
        public static int CalculateAge(this DateTime dateOfBirth)
        {
            //dateOfBirth = DateTime.ParseExact(dateOfBirth.ToString(), "dd/MM/yyyy HH:mm:ss", CultureInfo.CurrentCulture);
            // removed globalisation 
            var now = DateTime.Now;

            //now = DateTime.ParseExact(now.ToString(), "dd/MM/yyyy HH:mm:ss", CultureInfo.CurrentCulture);

            int years = new DateTime(DateTime.Now.Subtract(dateOfBirth).Ticks).Year - 1;

            return years;
        }
    }
}
