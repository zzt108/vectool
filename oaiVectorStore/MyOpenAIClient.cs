using OpenAI;
namespace oaiVectorStore
{

    public class MyOpenAIClient:IDisposable
    {

        public MyOpenAIClient()
        {
            Client = new OpenAIClient();
        }

        public MyOpenAIClient(string apiKey)
        {
            Client = new OpenAIClient(apiKey);
        }

        public OpenAIClient Client { get; set; }

        public void Dispose()
        {
            Client.Dispose();
        }
    }
}
