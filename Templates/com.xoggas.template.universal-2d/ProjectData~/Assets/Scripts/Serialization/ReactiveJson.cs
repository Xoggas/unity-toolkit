using System.Linq;
using Newtonsoft.Json;

namespace Serialization
{
    /// <summary>
    /// Entry point for the R3 / ObservableCollections converters.
    /// <code>
    /// var settings = ReactiveJson.CreateSettings();
    /// var json = JsonConvert.SerializeObject(save, settings);
    /// JsonConvert.PopulateObject(json, save, settings);
    /// </code>
    /// To make every JsonConvert call use them, call <see cref="UseAsDefault"/> once at startup.
    /// </summary>
    public static class ReactiveJson
    {
        public static JsonSerializerSettings CreateSettings() => new JsonSerializerSettings().AddReactiveConverters();

        public static JsonSerializerSettings AddReactiveConverters(this JsonSerializerSettings settings)
        {
            if (!settings.Converters.OfType<ReactivePropertyJsonConverter>().Any())
            {
                settings.Converters.Add(new ReactivePropertyJsonConverter());
            }

            if (!settings.Converters.OfType<ObservableCollectionJsonConverter>().Any())
            {
                settings.Converters.Add(new ObservableCollectionJsonConverter());
            }

            return settings;
        }

        public static void UseAsDefault()
        {
            var previous = JsonConvert.DefaultSettings;
            JsonConvert.DefaultSettings = () => (previous?.Invoke() ?? new JsonSerializerSettings()).AddReactiveConverters();
        }
    }
}
