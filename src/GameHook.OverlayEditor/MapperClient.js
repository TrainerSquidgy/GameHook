using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace GameHook.OverlayEditor {
    public class MapperMeta {
        public MapperValue gameName { get; set; }
}

public class MapperValue {
    public string value { get; set; }
    }

public class MapperResponse {
    public MapperMeta meta { get; set; }
    }

public static class MapperClient {
    private static readonly HttpClient http = new ()
        {
    BaseAddress = new Uri("http://localhost:8085")
};

        public static async Task < string ?> GetGameNameAsync()
        {
    try {
        var response = await http.GetFromJsonAsync < MapperResponse > ("/mapper");
        return response?.meta?.gameName?.value;
    }
    catch
    {
        return null;
    }
}
    }
}
