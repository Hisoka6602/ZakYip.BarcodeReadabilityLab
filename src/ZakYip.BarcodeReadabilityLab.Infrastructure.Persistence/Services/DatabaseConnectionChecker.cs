namespace ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Services;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;
using ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Data;

/// <summary>
/// 数据库连接检查器实现
/// </summary>
public class DatabaseConnectionChecker : IDatabaseConnectionChecker
{
    private readonly TrainingJobDbContext _dbContext;

    public DatabaseConnectionChecker(TrainingJobDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> CanConnectAsync()
    {
        try
        {
            return await _dbContext.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }
}
