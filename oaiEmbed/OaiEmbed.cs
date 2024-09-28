using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
//using System.Xml;

namespace oaiEmbed;
public class OaiEmbed
{
    static readonly HttpClient client = new HttpClient();

    static async Task Main(string[] args)
    {
        string csharpCode = "public class Test { public static void Main(string[] args) { Console.WriteLine(\"Hello, World!\"); } }";
        var embeddings = await GetEmbeddings(csharpCode);

        Console.WriteLine(JsonConvert.SerializeObject(embeddings, Formatting.Indented));
    }

    static async Task<dynamic> GetEmbeddings(string input)
    {
        var data = new
        {
            input = input,
            model = "text-embedding-ada-002"
        };

        string json = JsonConvert.SerializeObject(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.Add("Authorization", $"Bearer YOUR_OPENAI_API_KEY");
        var response = await client.PostAsync("https://api.openai.com/v1/embeddings", content);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<dynamic>(responseContent);
        }
        else
        {
            throw new Exception("Failed to get embeddings: " + response.StatusCode);
        }
    }
}