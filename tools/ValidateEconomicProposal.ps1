param([string]$ProposalPath=(Join-Path $PSScriptRoot '..\docs\proposals\initial-economy-2026-27.draft.json'))
# Valida únicamente documentación de propuesta; nunca abre ni escribe SQLite.
$ErrorActionPreference='Stop'
function Require([bool]$Condition,[string]$Message) { if(-not $Condition){throw $Message} }
$p=Get-Content -LiteralPath $ProposalPath -Raw -Encoding UTF8 | ConvertFrom-Json
$reference=Get-Content -LiteralPath (Join-Path $PSScriptRoot '..\docs\development\club-reference-2026-27.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$reputations=Get-Content -LiteralPath (Join-Path $PSScriptRoot '..\docs\decisions\club-reputations-2026-27.json') -Raw -Encoding UTF8 | ConvertFrom-Json
Require ($p.Status -eq 'AwaitingUserApproval' -and $p.RuntimeUseAllowed -eq $false) 'El borrador no debe autorizar uso en ejecución.'
Require ($p.Clubs.Count -eq 20) 'Se necesitan exactamente veinte clubes.'
Require (@($p.Clubs.ClubId | Sort-Object -Unique).Count -eq 20) 'Identidades repetidas.'
Require ($p.Currency -eq 'EUR' -and $p.LegalReference.FullTimeGrossAnnualMinimum -eq 17094 -and $p.LegalReference.Year -eq 2026) 'Referencia legal/moneda incorrecta.'
Require ($null -eq $p.LegalReference.Year2027Minimum) 'No extrapolar el mínimo de 2027.'
foreach($c in $p.Clubs){
    $ref=@($reference.Clubs | Where-Object FurvoClubId -eq $c.ClubId)
    Require ($ref.Count -eq 1) 'ClubId no registrado.'
    $guid=[guid]::Parse($c.ClubId);Require ($guid -ne [guid]::Empty) 'Identidad vacía.'
    Require ($c.Name -eq $ref[0].FurvoName -and $c.StadiumCapacity -eq $ref[0].Capacity) 'Nombre/aforo distinto de la referencia existente.'
    $rep=@($reputations.Clubs | Where-Object ClubId -eq $c.ClubId)[0].Internal
    if($c.ShortName -eq 'Fragua del Sur'){$rep=32} # Corrección aprobada posterior, sin editar el histórico.
    Require ($c.InternalReputation -eq $rep) 'Reputación inicial alterada.'
    Require ($c.Currency -eq 'EUR' -and $c.SquadSize -eq 24) 'Moneda/plantilla incorrecta.'
    foreach($field in @('InitialCash','TransferBudget','PlayerWageBudget','StaffBudget','YouthBudget','ShortTermDebt','LongTermDebt','TotalDebt','ProposedManagerGrossAnnualSalary','PlanningManagerEmployerCost')){
        Require ($null -ne $c.$field -and [decimal]$c.$field -ge 0) "Importe negativo o ausente: $field"
    }
    Require ($null -eq $c.CommittedPlayerWages -and $null -eq $c.PlayerWageRemaining) 'No inventar compromisos/margen real sin contratos salariales.'
    Require ($c.PlannedUnallocatedWageCapacity -ge 0 -and $c.PlannedUnallocatedWageCapacity -le $c.PlayerWageBudget) 'Reserva de planificación incoherente.'
    Require ($c.PlannedUnallocatedWageCapacity -eq $c.PlayerWageBudget-$c.PlannedPlayerWageExpense) 'Reserva no coincide con presupuesto menos previsión.'
    Require ($c.PlannedPlayerWageExpense -eq $c.PlayerWageBudget*$p.Assumptions.PlannedPlayerWageShare) 'Previsión salarial no coincide con escenario.'
    Require ($c.TotalDebt -eq $c.ShortTermDebt+$c.LongTermDebt) 'Componentes de deuda no suman.'
    Require ($c.ProposedManagerGrossAnnualSalary -gt $p.LegalReference.FullTimeGrossAnnualMinimum) 'Salario no superior al mínimo anual.'
    Require ($c.PlanningManagerEmployerCost -eq $c.ProposedManagerGrossAnnualSalary*$p.Assumptions.ManagerEmployerCostFactor) 'Provisión de coste de entrenador incoherente.'
    Require ($c.PlanningManagerEmployerCost -lt $c.ForecastExpense.SportingPersonnel -and $c.ForecastExpense.SportingPersonnel -le $c.StaffBudget) 'Entrenador no cubierto por staff.'
    Require ($c.ForecastExpense.SportingPersonnel -eq $c.StaffBudget*$p.Assumptions.PlannedStaffBudgetShare) 'Previsión de staff incoherente.'
    Require ($c.ForecastExpense.PlayerWages -eq $c.PlannedPlayerWageExpense -and $c.ForecastExpense.YouthDevelopment -eq $c.YouthBudget) 'Centros de coste incoherentes.'
    Require ($c.ForecastExpense.DebtInterest -eq $c.TotalDebt*$p.Assumptions.DebtInterestRate) 'Intereses incoherentes.'
    Require ($null -eq $c.ForecastIncome.PlayerSales -and $null -eq $c.ForecastExpense.PlayerPurchaseCashDisbursement -and $null -eq $c.ForecastExpense.PlayerPurchaseAccountingExpense) 'No inventar compraventas.'
    $income=[decimal]0;$expense=[decimal]0
    foreach($field in $c.ForecastIncome.PSObject.Properties){if($null -ne $field.Value){Require ([decimal]$field.Value -ge 0) 'Ingreso negativo.';$income+=[decimal]$field.Value}}
    foreach($field in $c.ForecastExpense.PSObject.Properties){if($null -ne $field.Value){Require ([decimal]$field.Value -ge 0) 'Gasto negativo.';$expense+=[decimal]$field.Value}}
    Require ($income -eq $c.RecurringIncomeForecast -and $expense -eq $c.RecurringExpenseForecast) 'Totales recurrentes incoherentes.'
    Require ($c.RecurringPlanningSurplus -eq $income-$expense) 'Superávit de planificación incorrecto.'
    Require ($c.StressClosingCash -eq $c.InitialCash+$income-$expense-$c.TransferBudget-$c.ShortTermDebt -and $c.StressClosingCash -ge 0) 'Caja de tensión incoherente o negativa.'
    Require ($c.PlanningSubscriberCount -lt $c.PlanningAverageAttendance -and $c.PlanningAverageAttendance -le $c.StadiumCapacity) 'Aforo/abonados de planificación incoherentes.'
    Require ($c.ActualTransactions.Count -eq 0) 'Una previsión no puede generar movimientos reales.'
}
Require (@($p.Clubs.PlayerWageBudget | Sort-Object -Unique).Count -gt 1) 'Todos los presupuestos son idénticos.'
Write-Output 'FURVO_ECONOMIC_PROPOSAL_OK: 20 clubes, identidades, reputaciones, EUR, salarios, staff, deuda y previsiones coherentes; sin importación SQLite.'
