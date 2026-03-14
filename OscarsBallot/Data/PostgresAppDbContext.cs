using Microsoft.EntityFrameworkCore;

namespace OscarsBallot.Data;

public class PostgresAppDbContext(DbContextOptions<PostgresAppDbContext> options) : AppDbContext(options);
