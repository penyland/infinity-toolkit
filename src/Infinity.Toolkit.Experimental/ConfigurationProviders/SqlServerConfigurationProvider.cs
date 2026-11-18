using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Infinity.Toolkit.Experimental.ConfigurationProviders;

public class SqlServerConfigurationSource : IConfigurationSource
{
    public string ConnectionString { get; set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new SqlServerConfigurationProvider(this);
    }
}

public class SqlServerConfigurationProvider(SqlServerConfigurationSource source) : ConfigurationProvider
{
    private readonly SqlServerConfigurationSource source = source;
    private const string Query = "SELECT [Key], [Value] FROM Configuration";

    public override void Load()
    {
        var dictionary = new Dictionary<string, string?>();
        using (var connection = new SqlConnection(source.ConnectionString))
        {
            var query = new SqlCommand(Query, connection);
            query.Connection.Open();

            using var reader = query.ExecuteReader();
            while (reader.Read())
            {
                dictionary.Add(reader.GetString(0), reader.GetString(1));
            }
        }

        Data = dictionary;
    }
}
