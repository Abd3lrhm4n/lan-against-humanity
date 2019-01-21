using Nancy;
using Nancy.Conventions;

namespace LahServer.Web
{
    class LahBootstrapper : DefaultNancyBootstrapper
    {
        public LahBootstrapper()
        {
        }

        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("/", "./web_content"));
            base.ConfigureConventions(nancyConventions);
        }

        //protected override IRootPathProvider RootPathProvider => new CustomRootPathProvider();

        private sealed class CustomRootPathProvider : IRootPathProvider
        {
            public string GetRootPath()
            {
                return "./web_content";
            }
        }
    }
}
