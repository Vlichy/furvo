namespace Furvo.Core.Careers;

/// <summary>Configuración explícita por país del club, nunca por nacionalidad del entrenador.</summary>
public sealed class ContractCountryConfigurationException(string? country):Exception(country)
{
    public string? Country { get; }=country;
}
public sealed record ContractCountryPolicy(string Country,string Currency,int LegalYear,decimal MinimumGrossAnnualSalary,string LegalSource);
public static class ContractCountryPolicies
{
    public static ContractCountryPolicy For(string? country)=>country switch
    {
        "España"=>new("España","EUR",2026,17094m,"https://www.boe.es/eli/es/rd/2026/02/18/126/con"),
        _=>throw new ContractCountryConfigurationException(country)
    };
}
/// <summary>Regla anual versionada. El margen adicional debe aprobarse; null no inventa un salario.
/// No introduce diferencias por antecedentes, reputación ni conversiones monetarias.</summary>
public sealed record InitialManagerSalaryRule(string Version,decimal? ApprovedAnnualPremium)
{
    public decimal? Calculate(ContractCountryPolicy country)=>
        !string.IsNullOrWhiteSpace(Version) && ApprovedAnnualPremium is >0
            ?country.MinimumGrossAnnualSalary+ApprovedAnnualPremium.Value:null;
}
/// <summary>Referencia congelada al ofertar; no reconsulta leyes al cargar contratos históricos.</summary>
public sealed record ManagerSalaryPolicySnapshot(ContractCountryPolicy Country,InitialManagerSalaryRule Rule);
public static class InitialManagerContractPolicy
{
    public static DateOnly Start=>new(2026,7,1);
    public static DateOnly End=>Start.AddYears(1).AddDays(-1);
    public static InitialManagerSalaryRule PendingSalaryRule { get; }=new("initial-salary-v1",null);
    // Las futuras renovaciones deberán producir nuevas condiciones explícitas, sin mutar contratos firmados.
}
