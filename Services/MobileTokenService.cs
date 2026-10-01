using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Shoppet_VetClinic.Models;

namespace Shoppet_VetClinic.Services;

public sealed class MobileTokenService(IConfiguration configuration, DatabaseService database)
{
    private readonly string _connectionString =
        configuration.GetConnectionString("ShoppetDb")
        ?? throw new InvalidOperationException("Connection string 'ShoppetDb' was not found.");

    public UserAccount? Authenticate(string email, string password)
    {
        var user = database.AuthenticateUser(email, password);
        return user is null ? null : Normalize(user);
    }

    public string Issue(UserAccount user, TimeSpan? lifetime = null)
    {
        if (user.Id <= 0)
            throw new InvalidOperationException("A valid user account is required.");

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var expiresAt = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromDays(30));
        var hash = Hash(token);

        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(@"
            UPDATE UserAccounts
            SET ApiToken = @Token,
                ApiTokenExpiresAt = @ExpiresAt
            WHERE Id = @UserId
              AND ISNULL(IsDisabled, 0) = 0;", connection);

        command.Parameters.AddWithValue("@Token", hash);
        command.Parameters.AddWithValue("@ExpiresAt", expiresAt);
        command.Parameters.AddWithValue("@UserId", user.Id);

        if (command.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("The mobile token could not be issued.");

        return token;
    }

    public UserAccount? Validate(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(@"
            SELECT Id, FullName, Email, Role, IsPremium
            FROM UserAccounts
            WHERE ApiToken = @Token
              AND ApiTokenExpiresAt > SYSUTCDATETIME()
              AND ISNULL(IsDisabled, 0) = 0;", connection);

        command.Parameters.AddWithValue("@Token", Hash(token));
        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return new UserAccount
        {
            Id = reader.GetInt32(0),
            FullName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            Email = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            Role = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            IsPremium = !reader.IsDBNull(4) && reader.GetBoolean(4)
        };
    }

    public bool Revoke(UserAccount user)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(@"
            UPDATE UserAccounts
            SET ApiToken = NULL,
                ApiTokenExpiresAt = NULL
            WHERE Id = @UserId;", connection);

        command.Parameters.AddWithValue("@UserId", user.Id);
        return command.ExecuteNonQuery() == 1;
    }

    private static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static UserAccount Normalize(UserAccount user)
    {
        user.Role = user.Role.Trim();
        return user;
    }
}
