using System.Reflection;
using System.Xml.Linq;
using Json.Schema;

namespace JsonEverythingNet.Services
{
    public class SchemaLocalizationService
    {

        private string? _currentCulture;
        private readonly Dictionary<string, Dictionary<string, string>> _resourceCache = new();
        private readonly object _lock = new();
        private readonly IHttpClientFactory _httpClientFactory;

        public SchemaLocalizationService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task SetCultureAsync(string cultureCode)
        {
            if (_currentCulture == cultureCode) return;

            Dictionary<string, string> resources;
            lock (_lock)
            {
                if (_resourceCache.TryGetValue(cultureCode, out resources!))
                {
                    UpdateErrorMessages(resources);
                    _currentCulture = cultureCode;
                    return;
                }
            }

            var cultureExtension = cultureCode == "en-US" ? string.Empty : $".{cultureCode}";
            var resxPath = $"/resources/Resources{cultureExtension}.resx";
            try
            {
                var client = _httpClientFactory.CreateClient("self");
				var stream = await client.GetStreamAsync(resxPath);
                var doc = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
                resources = doc.Root!.Elements("data")
                    .Where(e => e.Attribute("name") != null && e.Element("value") != null)
                    .ToDictionary(
                        e => e.Attribute("name")!.Value,
                        e => e.Element("value")!.Value
                    );
            }
            catch
            {
                resources = new Dictionary<string, string>();
            }
            lock (_lock)
            {
                _resourceCache[cultureCode] = resources;
            }
            UpdateErrorMessages(resources);
            _currentCulture = cultureCode;
        }

        private static void UpdateErrorMessages(Dictionary<string, string> resources)
        {
            var type = typeof(ErrorMessages);
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.CanWrite && p.PropertyType == typeof(string) || p.PropertyType == typeof(string));
            foreach (var prop in props)
            {
                var key = "Error_" + prop.Name;
                if (resources.TryGetValue(key, out var value))
                    prop.SetValue(null, value);
                else
                    prop.SetValue(null, null);
            }
        }
    }
}
