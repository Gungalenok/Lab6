using System.Text.Json;
using System.Text.Json.Serialization;
using Hospital.Domain;

namespace Hospital.Data;

public sealed class HospitalData
{
    public List<Patient> Patients { get; set; } = [];
    public List<StaffMember> Staff { get; set; } = [];
    public List<string> Departments { get; set; } = ["Кардиологическое", "Хирургическое", "Неврологическое", "Терапевтическое"];
}

public interface IHospitalRepository
{
    HospitalData Load();
    void Save(HospitalData data);
}

public sealed class JsonHospitalRepository : IHospitalRepository
{
    private readonly string _path;
    private readonly JsonSerializerOptions _options = new() {
        WriteIndented = true, //отступы JSON
        Converters = { new JsonStringEnumConverter() } //сохраняем енумы строками
    };
    public JsonHospitalRepository(string? path = null) => _path = path ?? Path.Combine(AppContext.BaseDirectory, "data", "hospital.json");
    public HospitalData Load()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        if (!File.Exists(_path)) { 
            var initial = CreateInitialData(); 
            Save(initial); 
            return initial; 
        }
        try { 
            return JsonSerializer.Deserialize<HospitalData>(File.ReadAllText(_path), _options) 
                ?? new HospitalData(); //если десериализация вернула null(например, файл содержит null), вернуть пустой объект.Защита от NullReferenceException
        }
        catch (JsonException ex) { 
            throw new InvalidDataException("Файл данных поврежден. Исправьте или удалите data/hospital.json.", ex); 
        }
    }
    public void Save(HospitalData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(data, _options));
        File.Move(temp, _path, true); //атомарная запись: если процесс упадёт во время записи, старый файл не повреждён — он ещё не переименован.
    }
    private static HospitalData CreateInitialData()
    {
        var d = new HospitalData();
        d.Staff.AddRange([
            new StaffMember { FullName="Иванова Мария Петровна", Role=StaffRole.DutyDoctor, Department="Приемное отделение" },
            new StaffMember { FullName="Смирнов Алексей Викторович", Role=StaffRole.Doctor, Department="Кардиологическое" },
            new StaffMember { FullName="Кузнецова Ольга Сергеевна", Role=StaffRole.Doctor, Department="Хирургическое" },
            new StaffMember { FullName="Петрова Анна Игоревна", Role=StaffRole.Nurse, Department="Кардиологическое" },
            new StaffMember { FullName="Соколова Елена Андреевна", Role=StaffRole.Nurse, Department="Хирургическое" }
        ]);
        return d;
    }
}
