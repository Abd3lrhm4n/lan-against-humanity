using Nancy;

namespace LahServer.Web
{
    public class IndexModule : NancyModule
    {
        public IndexModule() : base("")
        {
            Get["/"] = p => Response.AsFile($"./web_content/index.html", "text/html");
        }
    }
}
