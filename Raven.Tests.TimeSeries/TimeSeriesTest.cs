using Raven.Abstractions.TimeSeries;
using Raven.Database.Config;
using Raven.Database.TimeSeries;

namespace Raven.Tests.TimeSeries
{
    public class TimeSeriesTest
    {
        public static TimeSeriesStorage GetStorage()
        {
            var storage = new TimeSeriesStorage("http://localhost:8080/", "TimeSeriesTest", new AppSettingsBasedConfiguration { Core = { RunInMemory = true }});
            using (var writer = storage.CreateWriter())
            {
                writer.CreateType(new TimeSeriesType {Type = "Simple", Fields = new[] {"Value"}});
            }
            return storage;
        }
    }
}
