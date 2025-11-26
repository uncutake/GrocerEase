using Microsoft.AspNetCore.Mvc;
using Dapper;
using MySqlConnector;

namespace Recipes.Api.Controllers
{
    [ApiController]
    [Route("favorites")]
    public class FavoritesController : ControllerBase
    {
        private readonly MySqlConnection _conn;

        public FavoritesController(MySqlConnection conn)
        {
            _conn = conn;
        }

        [HttpGet("check")]
        public async Task<bool> CheckFavorite(int userId, int recipeId)
        {
            const string sql = @"SELECT COUNT(*) FROM Favorites 
                                 WHERE UserId = @userId AND RecipeId = @recipeId";

            var count = await _conn.ExecuteScalarAsync<int>(sql, new { userId, recipeId });
            return count > 0;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddFavorite(int userId, int recipeId)
        {
            const string sql = @"INSERT INTO Favorites (UserId, RecipeId)
                                 VALUES (@userId, @recipeId)";

            await _conn.ExecuteAsync(sql, new { userId, recipeId });
            return Ok();
        }

        [HttpPost("remove")]
        public async Task<IActionResult> RemoveFavorite(int userId, int recipeId)
        {
            const string sql = @"DELETE FROM Favorites 
                                 WHERE UserId = @userId AND RecipeId = @recipeId";

            await _conn.ExecuteAsync(sql, new { userId, recipeId });
            return Ok();
        }
    }
}