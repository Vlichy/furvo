using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Core.Worlds;
using Microsoft.Data.Sqlite;

namespace Furvo.Persistence;

/// <summary>Cada carrera es una copia transaccional independiente. Una solicitud identifica una sola publicación idempotente.</summary>
public sealed class SqliteCareerStore : ICareerStore
{
    public const int CurrentCareerSchemaVersion=2;
    private readonly string _directory;private readonly string _initial;
    public SqliteCareerStore(string directory,string initialWorld) { _directory=WorldDatabase.PathFor(directory);_initial=WorldDatabase.PathFor(initialWorld); }
    public string PathFor(SaveId id) { if(id.Value==Guid.Empty)throw new ArgumentException("Identidad vacía.");return Path.Combine(_directory,id+".db"); }
    public CareerSummary Create(CareerCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);ArgumentNullException.ThrowIfNull(request.ProfileSnapshot);
        request=request with{Offer=request.Offer?.Freeze()};
        var initial=new SqliteWorldStore(_initial);var info=initial.ReadInfo();var world=initial.Load();
        if(info.Kind!=WorldFileKind.InitialWorld || world.Seasons.Count!=1)throw new InvalidOperationException("Se requiere la publicación inicial con una temporada.");
        var season=world.Seasons[0];var date=season.StartDate;Validate(request,world,date);
        var requestJson=CareerJson.Write(request);
        var sourceHash=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(_initial)));
        var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(requestJson+sourceHash))).ToLowerInvariant();
        var destination=PathFor(request.Id);
        CareerSummary Existing()
        {
            using var c=WorldDatabase.Open(destination);using var t=c.BeginTransaction(deferred:true);
            using var cmd=WorldDatabase.Command(c,t,"SELECT request_hash FROM career_state WHERE singleton=1;");
            if(cmd.ExecuteScalar() as string!=hash)throw new InvalidOperationException("El destino pertenece a otra solicitud; no se sobrescribe.");
            return Load(request.Id);
        }
        if(File.Exists(destination))return Existing();
        Directory.CreateDirectory(_directory);
        try
        {
            using var source=WorldDatabase.Open(_initial);using var snapshot=source.BeginTransaction(deferred:true);
            _=WorldDatabase.Info(source,snapshot);
            SqliteWorldFiles.Publish(destination,target=>
            {
                source.BackupDatabase(target);using var t=target.BeginTransaction();
                using(var metadata=WorldDatabase.Command(target,t,"UPDATE world_info SET kind='save' WHERE singleton=1; INSERT INTO save_metadata(singleton,save_id) VALUES(1,$id);",("$id",request.Id.ToString())))metadata.ExecuteNonQuery();
                CareerJson.Migrate(target,t,"Career001_Initial.sql");
                CareerJson.Migrate(target,t,"Career002_ContractTerms.sql");
                using(var coach=WorldDatabase.Command(target,t,"INSERT INTO career_coaches VALUES($id,$profile,$json);",("$id",request.CoachId.ToString()),("$profile",request.ProfileSnapshot.Id.ToString()),("$json",CareerJson.Write(request.ProfileSnapshot))))coach.ExecuteNonQuery();
                var employed=request.Mode==CareerStartMode.SignOffer;
                if(employed)
                {
                    var offer=request.Offer!.ForSignature();var terms=offer.Terms;
                    SqliteEconomyStore.CommitManager(target,t,offer);
                    using var contract=WorldDatabase.Command(target,t,"INSERT INTO manager_contracts(id,career_singleton,coach_id,club_id,start_date,end_date,gross_annual_salary,currency,termination_kind,termination_conditions,offer_json) VALUES($id,1,$coach,$club,$start,$end,$salary,$currency,$termination_kind,$termination,$json);",
                        ("$id",request.ContractId!.Value.ToString()),("$coach",request.CoachId.ToString()),("$club",offer.ClubId.ToString()),
                        ("$start",Date(terms.StartDate!.Value)),("$end",Date(terms.EndDate!.Value)),("$termination_kind",(int)terms.TerminationKind),
                        ("$salary",terms.GrossAnnualSalary!.Value.ToString(CultureInfo.InvariantCulture)),("$currency",terms.Currency),("$termination",terms.TerminationConditions),("$json",CareerJson.Write(offer)));
                    contract.ExecuteNonQuery();
                }
                if(request.Mode==CareerStartMode.RejectOffer)
                {
                    using var decline=WorldDatabase.Command(target,t,"INSERT INTO declined_manager_offers VALUES($id,1,$club,$json);",("$id",request.Offer!.Id.ToString("D")),("$club",request.Offer.ClubId.ToString()),("$json",CareerJson.Write(request.Offer.Freeze())));decline.ExecuteNonQuery();
                }
                using(var state=WorldDatabase.Command(target,t,"INSERT INTO career_state VALUES(1,$id,$coach,$season,$date,$mode,$employment,$club,$contract,$hash);",
                    ("$id",request.Id.ToString()),("$coach",request.CoachId.ToString()),("$season",season.Id.ToString()),("$date",Date(date)),("$mode",(int)request.Mode),
                    ("$employment",employed?2:1),("$club",employed?request.Offer!.ClubId.ToString():null),("$contract",request.ContractId?.ToString()),("$hash",hash)))state.ExecuteNonQuery();
                SqliteLineupStore.Initialize(target,t,world,request.Id,employed?request.Offer!.ClubId:null,date);
                using(var check=WorldDatabase.Command(target,t,"PRAGMA foreign_key_check;"))using(var rows=check.ExecuteReader())if(rows.Read())throw new InvalidDataException("Referencias incoherentes en la carrera.");
                t.Commit();
            });
        }
        catch(IOException) when(File.Exists(destination)) { return Existing(); }
        return Load(request.Id);
    }
    private static void Validate(CareerCreationRequest r,WorldData world,DateOnly date)
    {
        if(r.Id.Value==Guid.Empty || r.CoachId.Value==Guid.Empty || r.CoachId.Value==r.ProfileSnapshot.Id.Value || !Enum.IsDefined(r.Mode))throw new ArgumentException("Identidad o modo no válido.");
        r.ProfileSnapshot.ValidateAge(date);
        if(r.Mode==CareerStartMode.Unemployed)
        { if(r.Offer is not null || r.ContractId is not null)throw new InvalidOperationException("El paro directo no acepta club ni contrato.");return; }
        if(r.Offer is not {} offer || offer.Id==Guid.Empty || offer.ProfileId!=r.ProfileSnapshot.Id || !world.Clubs.Any(c=>c.Id==offer.ClubId) || offer.SeasonId!=world.Seasons[0].Id)
            throw new InvalidOperationException("La oferta tiene referencias incompatibles.");
        if(r.Mode==CareerStartMode.RejectOffer)
        {if(r.ContractId is not null)throw new InvalidOperationException("Rechazar no crea contrato.");return;}
        if(r.ContractId is not {} id || id.Value==Guid.Empty || offer.SigningBlocks().Count!=0 || offer.Terms.StartDate!=date)
            throw new InvalidOperationException("Contrato incompleto o no aprobado.");
    }
    public CareerSummary Load(SaveId id)
    {
        var path=PathFor(id);using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);var info=WorldDatabase.Info(c,t);
        if(info.Kind!=WorldFileKind.Save || info.SaveId!=id)throw new InvalidDataException("No es la carrera solicitada.");
        int snapshotVersion;
        using(var version=WorldDatabase.Command(c,t,"SELECT version FROM career_schema_version WHERE singleton=1;"))snapshotVersion=Convert.ToInt32(version.ExecuteScalar());
        if(snapshotVersion is not (1 or 2))throw new NotSupportedException("Versión de carrera no compatible.");
        using var cmd=WorldDatabase.Command(c,t,"SELECT s.coach_id,s.season_id,s.current_date,s.employment,s.club_id,s.contract_id,c.profile_id,c.profile_json,s.start_mode FROM career_state s JOIN career_coaches c ON s.coach_id=c.id WHERE s.singleton=1;");
        CoachId coach;SeasonId season;DateOnly date;CareerEmploymentState employment;ClubId? club;ManagerContractId? contractId;CoachProfile profile;int mode;
        using(var r=cmd.ExecuteReader())
        {
            if(!r.Read())throw new InvalidDataException("Falta el estado inicial de carrera.");
            coach=new CoachId(Guid.Parse(r.GetString(0)));season=new SeasonId(Guid.Parse(r.GetString(1)));date=DateOnly.ParseExact(r.GetString(2),"yyyy-MM-dd",CultureInfo.InvariantCulture);
            employment=(CareerEmploymentState)r.GetInt32(3);club=r.IsDBNull(4)?null:new ClubId(Guid.Parse(r.GetString(4)));contractId=r.IsDBNull(5)?null:new ManagerContractId(Guid.Parse(r.GetString(5)));
            profile=CareerJson.Read<CoachProfile>(r.GetString(7));if(profile.Id.ToString()!=r.GetString(6))throw new InvalidDataException("Perfil de instantánea incompatible."); mode=r.GetInt32(8);
        }
        ManagerContract? contract=null;ManagerOffer? rejected=null;
        if(contractId is {} cid)
        {
            using var contractCmd=WorldDatabase.Command(c,t,"SELECT offer_json FROM manager_contracts WHERE id=$id AND coach_id=$coach AND club_id=$club;",("$id",cid.ToString()),("$coach",coach.ToString()),("$club",club?.ToString()));
            var offer=CareerJson.Read<ManagerOffer>(contractCmd.ExecuteScalar() as string??throw new InvalidDataException("Falta el contrato activo."));
            // La lectura v1 conserva sus condiciones históricas sin aplicar reglas v2 ni escribir el archivo.
            if(snapshotVersion==1 && !string.IsNullOrWhiteSpace(offer.Terms.TerminationConditions))
                offer=offer with{Terms=offer.Terms with{TerminationKind=TerminationClauseKind.Defined}};
            var termsValid=snapshotVersion==2?offer.SigningBlocks().Count==0:
                offer.Terms.StartDate.HasValue && offer.Terms.EndDate.HasValue && offer.Terms.EndDate>=offer.Terms.StartDate && offer.Terms.GrossAnnualSalary>0 && !string.IsNullOrWhiteSpace(offer.Terms.Currency);
            if(!termsValid || offer.ClubId!=club || offer.ProfileId!=profile.Id || offer.SeasonId!=season)throw new InvalidDataException("Contrato inválido.");
            contract=new ManagerContract(cid,coach,offer.Freeze(),snapshotVersion);
        }
        using(var declined=WorldDatabase.Command(c,t,"SELECT offer_json FROM declined_manager_offers WHERE career_singleton=1;"))if(declined.ExecuteScalar() is string json)rejected=CareerJson.Read<ManagerOffer>(json).Freeze();
        if((employment==CareerEmploymentState.Employed)!=(club is not null && contract is not null) || (employment==CareerEmploymentState.Unemployed && (club is not null || contract is not null)) || ((mode==2)!=(rejected is not null)))throw new InvalidDataException("Estado laboral incoherente.");
        var economic=SqliteEconomyStore.ReadResources(c,t);
        if(economic.Count>0)
        {
            if(economic.Count!=20 || economic.Any(e=>e.SeasonId!=season))throw new InvalidDataException("Estado económico incompleto.");
            var committed=economic.Where(e=>e.ManagerCommittedAnnualCost is not null).ToArray();
            if(contract is null ? committed.Length!=0 : committed.Length!=1 || committed[0].ClubId!=club ||
               committed[0].ManagerCommittedAnnualCost!=contract.AcceptedOffer.Resources.EconomicPlan?.PlanningManagerEmployerCost)
                throw new InvalidDataException("Compromiso económico incompatible con contrato.");
        }
        return new CareerSummary(id,info.WorldId,season,date,new CareerCoach(coach,profile),employment,club,contract,rejected);
    }
    private static string Date(DateOnly date)=>date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
}
