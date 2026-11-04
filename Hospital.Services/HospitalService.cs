using Hospital.Data;
using Hospital.Domain;

namespace Hospital.Services;

public sealed class HospitalService(IHospitalRepository repository)
{
    private readonly HospitalData _data = repository.Load();
    private void Persist() => repository.Save(_data);
    public IReadOnlyList<Patient> Patients => _data.Patients.OrderByDescending(x => x.AdmittedAt).ToList();
    public IReadOnlyList<string> Departments => _data.Departments;
    public IReadOnlyList<StaffMember> Doctors => _data.Staff.Where(x => x.Role is StaffRole.Doctor or StaffRole.DutyDoctor).ToList();
    public IReadOnlyList<StaffMember> Nurses => _data.Staff.Where(x => x.Role == StaffRole.Nurse).ToList();
    public Patient Admit(Patient patient) 
    {
        patient.Validate(); 
        patient.AdmittedAt = DateTime.Now; 
        patient.MedicalRecordNumber = $"ЭМК-{DateTime.Now:yyyyMMdd}-{_data.Patients.Count + 1:000}";
        _data.Patients.Add(patient); 
        Persist(); 
        return patient;
    }
    public void AddOrder(Guid patientId, MedicalOrder order)
    {
        var patient = Active(patientId); 
        order.Validate(); 
        patient.Orders.Add(order); 
        Persist();
    }
    public void CompleteOrder(Guid patientId, Guid orderId, Guid staffId)
    {
        var patient = Active(patientId); 
        var order = patient.Orders.Single(x => x.Id == orderId);
        if (order.CompletedAt.HasValue) throw new ValidationException("Назначение уже выполнено.");
        var worker = _data.Staff.Single(x => x.Id == staffId);
        if (order.Type is OrderType.Injection or OrderType.Tablets ? worker.Role != StaffRole.Nurse : worker.Role is not (StaffRole.Doctor or StaffRole.DutyDoctor))
            throw new ValidationException(order.Type is OrderType.Injection or OrderType.Tablets ? "Медикаментозное назначение выполняет медсестра." : "Диагностическое и профилактическое назначение выполняет врач.");
        order.CompletedAt = DateTime.Now; 
        order.PerformedBy = worker.FullName; 
        Persist();
    }
    public void Discharge(Guid patientId)
    {
        var patient = Active(patientId);
        if (patient.Orders.Any(x => !x.CompletedAt.HasValue)) throw new ValidationException("Нельзя выписать пациента: остались невыполненные назначения.");
        patient.DischargedAt = DateTime.Now; 
        Persist();
    }
    public Patient GetPatient(Guid id) => _data.Patients.Single(x => x.Id == id);
    public string CreateDischargeSummary(Guid id)
    {
        var p = GetPatient(id); 
        if (!p.IsDischarged) throw new ValidationException("Эпикриз формируется после выписки.");
        var lines = new List<string> { "ВЫПИСНОЙ ЭПИКРИЗ", "", $"ЭМК: {p.MedicalRecordNumber}", $"Пациент: {p.FullName}", $"Дата рождения: {p.BirthDate:dd.MM.yyyy}", $"Пол: {p.Gender}", $"Телефон: {p.Phone}", $"Адрес: {p.Address}", $"Период стационарного лечения: {p.AdmittedAt:dd.MM.yyyy HH:mm} — {p.DischargedAt:dd.MM.yyyy HH:mm}", $"Отделение: {p.Department}", $"Диагноз при поступлении: {p.AdmissionDiagnosis}", "", "Выполненные назначения:" };
        lines.AddRange(p.Orders.Where(x => x.CompletedAt.HasValue).Select(x => $"• {x.Name} ({TypeName(x.Type)}{FormatDetails(x)}) — {x.CompletedAt:dd.MM.yyyy HH:mm}, выполнил(а): {x.PerformedBy}"));
        if (!p.Orders.Any(x => x.CompletedAt.HasValue)) lines.Add("Нет");
        return string.Join(Environment.NewLine, lines);
    }
    public static string TypeName(OrderType type) => type switch { 
        OrderType.Injection => "Инъекция", 
        OrderType.Tablets => "Таблетки", 
        OrderType.Diagnostic => "Диагностика", 
        _ => "Профилактика" };
    public static string FormatDetails(MedicalOrder o) => o.Type switch { 
        OrderType.Injection => $" количество: {o.Quantity}", 
        OrderType.Tablets => $" дозировка: {o.Dosage} дней: {o.Days}", 
        _ => string.IsNullOrWhiteSpace(o.Details) ? "" : $"; {o.Details}" };
    private Patient Active(Guid id) { 
        var p=GetPatient(id); 
        if (p.IsDischarged) throw new ValidationException("Пациент уже выписан."); 
        return p; 
    }
}