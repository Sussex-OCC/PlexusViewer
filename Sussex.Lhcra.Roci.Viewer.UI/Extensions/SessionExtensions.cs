using Microsoft.AspNetCore.Http;
using System;
using System.Globalization;
using System.Text.Json;

namespace Sussex.Lhcra.Roci.Viewer.UI.Extensions
{
    public static class SessionExtensions
    {
        public static int CalculateAge(this DateTime dateOfBirth)
        {
            dateOfBirth = DateTime.ParseExact(dateOfBirth.ToString(), "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

            var now = DateTime.Now;

            now = DateTime.ParseExact(now.ToString(), "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

            int years = new DateTime(DateTime.Now.Subtract(dateOfBirth).Ticks).Year - 1;

            return years;
        }
        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}
