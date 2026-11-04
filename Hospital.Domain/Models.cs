namespace Hospital.Domain;

public enum StaffRole { DutyDoctor, Doctor, Nurse }
public enum OrderType { Injection, Tablets, Diagnostic, Preventive }
public enum OrderStatus { Planned, Completed }

public sealed class ValidationException(string message) : Exception(message);
public abstract class BusinessObject
{
    public virtual void Validate() { }
    protected static void Required(string? value, string label)
    { if (string.IsNullOrWhiteSpace(value)) throw new ValidationException($"Заполните поле «{label}»."); }
}

public sealed class Patient : BusinessObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = "";
    public DateTime BirthDate { get; set; }
    public string Gender { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public DateTime AdmittedAt { get; set; } = DateTime.Now;
    public DateTime? DischargedAt { get; set; }
    public string AdmissionDiagnosis { get; set; } = "";
    public string Department { get; set; } = "";
    public Guid AttendingDoctorId { get; set; }
    public string AttendingDoctor { get; set; } = "";
    public string MedicalRecordNumber { get; set; } = "";
    public List<MedicalOrder> Orders { get; set; } = [];
    public bool IsDischarged => DischargedAt.HasValue;
    public override void Validate()
    {
        Required(FullName, "ФИО"); Required(Gender, "Пол"); Required(AdmissionDiagnosis, "Диагноз при поступлении");
        Required(Department, "Отделение"); Required(AttendingDoctor, "Лечащий врач");
        if (BirthDate.Date > DateTime.Today) throw new ValidationException("Дата рождения не может быть в будущем.");
        if (DischargedAt.HasValue && DischargedAt < AdmittedAt) throw new ValidationException("Дата выписки раньше даты поступления.");
    }
}

public sealed class StaffMember : BusinessObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = "";
    public StaffRole Role { get; set; }
    public string Department { get; set; } = "";
    public override void Validate() { Required(FullName, "ФИО сотрудника"); Required(Department, "Отделение"); }
    public override string ToString() => FullName;
}

public sealed class MedicalOrder : BusinessObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public OrderType Type { get; set; }
    public string Name { get; set; } = "";
    public string Details { get; set; } = "";
    public int? Quantity { get; set; }
    public decimal? Dosage { get; set; }
    public int? Days { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }
    public string PerformedBy { get; set; } = "";
    public OrderStatus Status => CompletedAt.HasValue ? OrderStatus.Completed : OrderStatus.Planned;
    public override void Validate()
    {
        Required(Name, "Название назначения");
        if (Type == OrderType.Injection && (!Quantity.HasValue || Quantity < 1)) throw new ValidationException("Для инъекций укажите количество не менее 1.");
        if (Type == OrderType.Tablets && (!Dosage.HasValue || Dosage <= 0 || !Days.HasValue || Days < 1)) throw new ValidationException("Для таблеток укажите дозировку и число дней приема.");
    }
}
