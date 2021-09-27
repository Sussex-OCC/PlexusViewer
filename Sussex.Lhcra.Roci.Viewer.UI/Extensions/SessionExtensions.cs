using Microsoft.AspNetCore.Http;
using System;
using System.Text.Json;

namespace Sussex.Lhcra.Roci.Viewer.UI.Extensions
{
    public static class SessionExtensions
    {
        public static int CalculateAge(this DateTime dateOfBirth)
        {
            DateTime Now = DateTime.Now;
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
