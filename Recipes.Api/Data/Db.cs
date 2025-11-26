using MySqlConnector;
using Dapper;

namespace Recipes.Api.Data;

public class Db
{
    private readonly IConfiguration _config;

    public Db(IConfiguration config)
    {
        _config = config;
    }

    public MySqlConnection GetConnection()
    {
        return new MySqlConnection(_config.GetConnectionString("MySql"));
    }
}