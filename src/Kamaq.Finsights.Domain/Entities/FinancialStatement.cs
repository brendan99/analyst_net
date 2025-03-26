using System;
using System.Collections.Generic;

namespace Kamaq.Finsights.Domain.Entities;

public enum StatementType
{
    IncomeStatement,
    BalanceSheet,
    CashFlowStatement
}

public enum ReportingPeriod
{
    Annual,
    Quarterly
}

public class FinancialStatement
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public StatementType StatementType { get; private set; }
    public ReportingPeriod Period { get; private set; }
    public DateTime FilingDate { get; private set; }
    public DateTime PeriodEndDate { get; private set; }
    public string? FiscalYear { get; private set; }
    public string? FiscalQuarter { get; private set; }
    public string FilingUrl { get; private set; } = string.Empty;
    public string AccessionNumber { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    
    // Nested collection of financial data points
    private readonly List<FinancialDataPoint> _dataPoints = new();
    public IReadOnlyCollection<FinancialDataPoint> DataPoints => _dataPoints.AsReadOnly();
    
    // Navigation property
    public Company? Company { get; private set; }
    
    // Required for EF Core
    private FinancialStatement() { }
    
    public FinancialStatement(
        Guid companyId,
        string ticker,
        StatementType statementType,
        ReportingPeriod period,
        DateTime filingDate,
        DateTime periodEndDate,
        string filingUrl,
        string accessionNumber,
        string? fiscalYear = null,
        string? fiscalQuarter = null)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        Ticker = ticker;
        StatementType = statementType;
        Period = period;
        FilingDate = filingDate;
        PeriodEndDate = periodEndDate;
        FiscalYear = fiscalYear;
        FiscalQuarter = fiscalQuarter;
        FilingUrl = filingUrl;
        AccessionNumber = accessionNumber;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void AddDataPoint(string name, string value, string unit)
    {
        var dataPoint = new FinancialDataPoint(Id, name, value, unit);
        _dataPoints.Add(dataPoint);
    }
}

public class FinancialDataPoint
{
    public Guid Id { get; private set; }
    public Guid FinancialStatementId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    
    // Navigation property
    public FinancialStatement? FinancialStatement { get; private set; }
    
    // Required for EF Core
    private FinancialDataPoint() { }
    
    public FinancialDataPoint(
        Guid financialStatementId,
        string name,
        string value,
        string unit)
    {
        Id = Guid.NewGuid();
        FinancialStatementId = financialStatementId;
        Name = name;
        Value = value;
        Unit = unit;
        CreatedAt = DateTime.UtcNow;
    }
} 