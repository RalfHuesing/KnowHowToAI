namespace KnowHowToAI.Server.Web.Features.Dashboard;

/// <summary>Neutrale, korrelierbare Diagnose eines isoliert fehlgeschlagenen Dashboardbereichs.</summary>
public sealed record DashboardAreaError(string Code, string CorrelationId);
