﻿using MyWallet.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyWallet.Models;
namespace MyWallet.Services
{
    public interface IUserService
    {
        Task<User> GetUserByIdAsync(int id);
        Task<User> GetUserByEmailAsync(string email);
        Task<User> GetUserByUsernameAsync(string username);
        Task<bool> CreateUserAsync(User user, string password);
        Task<bool> ValidateUserCredentialsAsync(string username, string password);
        Task<bool> UpdateUserAsync(User user);
        Task<string> GetUserEmailByPortfolioIdAsync(int portfolioId);
    }

    public interface IPortfolioService
    {
        Task<IEnumerable<Portfolio>> GetUserPortfoliosAsync(int userId);
        Task<Portfolio> GetPortfolioByIdAsync(int id);
        Task<Portfolio> CreatePortfolioAsync(Portfolio portfolio);
        Task<bool> UpdatePortfolioAsync(Portfolio portfolio);
        Task<bool> DeletePortfolioAsync(int id);
        Task<decimal> CalculatePortfolioValueAsync(int id);
        Task<PortfolioHistory> RecordPortfolioHistoryAsync(int portfolioId);
        Task<IEnumerable<PortfolioHistory>> GetPortfolioHistoryAsync(int portfolioId, DateTime startDate, DateTime endDate);
        Task<Dictionary<string, decimal>> GetAssetCategoryDistributionAsync(int portfolioId);
        Task<decimal> GetInvestedAmountAsync(int portfolioId);
        Task<decimal> GetPortfolioProfitLossAsync(int portfolioId);
        Task<bool> PortfolioExistsAsync(int portfolioId);
        Task<byte[]> GeneratePortfolioChartWithOxyPlotAsync(int portfolioId, DateTime start, DateTime end);
        Task<List<User>> GetAllUsersAsync();
        Task<byte[]> GeneratePortfolioReportPdfAsync(int userId);
    }

    public interface IAssetService
    {
        Task<IEnumerable<Asset>> GetPortfolioAssetsAsync(int portfolioId);
        Task<Asset> GetAssetByIdAsync(int id);
        Task<Asset> CreateAssetAsync(Asset asset, decimal? userPrice = null);
    
        Task<bool> UpdateAssetAsync(Asset asset);
        Task<bool> DeleteAssetAsync(int id);
        Task<decimal> CalculateAssetCurrentValueAsync(int id);
        Task<decimal> CalculateAssetProfitLossAsync(int id);
        Task UpdateAssetPricesAsync(int portfolioId);
        Task<IEnumerable<AssetPriceHistory>> GetAssetPriceHistoryAsync(int assetId, DateTime startDate, DateTime endDate);
        Task<Asset> SellAssetAsync(int assetId, decimal quantityToSell, decimal price);
    }


    public interface ITransactionService
    {
        Task<IEnumerable<Transaction>> GetPortfolioTransactionsAsync(int portfolioId);
        Task<Transaction> GetTransactionByIdAsync(int id);
        Task<Transaction> CreateTransactionAsync(Transaction transaction);
        Task<bool> UpdateTransactionAsync(Transaction transaction);
        Task<bool> DeleteTransactionAsync(int id);
        Task<IEnumerable<Transaction>> GetTransactionsByAssetAsync(int assetId);
        Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(int portfolioId, DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalInvestedAmountAsync(int portfolioId);
        Task<decimal> GetTotalWithdrawnAmountAsync(int portfolioId);
        Task<byte[]> GenerateReportPdfAsync(int portfolioId, DateTime start, DateTime end);
        Task SendReportPdfByEmailAsync(int portfolioId, DateTime start, DateTime end);
    }

    public interface IReportService
    {
        Task<byte[]> GeneratePortfolioSummaryReportAsync(int portfolioId);
        Task<byte[]> GenerateTransactionHistoryReportAsync(int portfolioId, DateTime? startDate, DateTime? endDate);
        Task<byte[]> GenerateAssetPerformanceReportAsync(int portfolioId);
        Task<byte[]> GeneratePortfolioPerformanceChartAsync(int portfolioId, DateTime startDate, DateTime endDate);
        Task<byte[]> GenerateAssetDistributionChartAsync(int portfolioId);
    }

    public interface IExternalApiService
    {
        Task<decimal> GetCurrentPriceAsync(string symbol, string category);
        Task<Dictionary<string, decimal>> GetMultipleCurrentPricesAsync(IEnumerable<string> symbols, string category);
        Task<IEnumerable<AssetHintDto>> SearchAssetsAsync(string query, string category);
        Task<Dictionary<DateTime, decimal>> GetHistoricalPricesAsync(string symbol, string category,
            DateTime startDate, DateTime endDate);
    }
    
    public interface IEmailService
    {
        Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachmentBytes, string attachmentName);
    }
}