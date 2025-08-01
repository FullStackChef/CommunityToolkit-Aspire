//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
namespace Aspire.Hosting
{
    public static partial class SqliteResourceBuilderExtensions
    {
        public static ApplicationModel.IResourceBuilder<ApplicationModel.SqliteResource> AddSqlite(this IDistributedApplicationBuilder builder, string name, string? databasePath = null, string? databaseFileName = null) { throw null; }

        public static ApplicationModel.IResourceBuilder<ApplicationModel.SqliteResource> WithSqliteWeb(this ApplicationModel.IResourceBuilder<ApplicationModel.SqliteResource> builder, System.Action<ApplicationModel.IResourceBuilder<ApplicationModel.SqliteWebResource>>? configureContainer = null, string? containerName = null) { throw null; }
    }
}

namespace Aspire.Hosting.ApplicationModel
{
    public partial class SqliteResource : Resource, IResourceWithConnectionString, IResource, IManifestExpressionProvider, IValueProvider, IValueWithReferences
    {
        public SqliteResource(string name, string databasePath, string databaseFileName) : base(default!) { }

        public ReferenceExpression ConnectionStringExpression { get { throw null; } }
    }

    public partial class SqliteWebResource : ContainerResource, IResourceWithConnectionString, IResource, IManifestExpressionProvider, IValueProvider, IValueWithReferences
    {
        public SqliteWebResource(string name) : base(default!, default) { }

        public ReferenceExpression ConnectionStringExpression { get { throw null; } }

        public EndpointReference PrimaryEndpoint { get { throw null; } }
    }
}