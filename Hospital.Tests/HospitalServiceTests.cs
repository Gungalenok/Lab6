using Xunit;
using Hospital.Data;
using Hospital.Domain;
using Hospital.Services;

namespace Hospital.Tests;

public sealed class HospitalServiceTests
{
    [Fact]
    public void MedicationOrder_CanOnlyBeCompletedByNurse()
    {
        var service = CreateService(out var doctor, out var nurse);
        var patient = service.Admit(BusinessValidationTests.CreateValidPatient());
        var order = new MedicalOrder { Type = OrderType.Injection, Name = "Антибиотик", Quantity = 3 };
        service.AddOrder(patient.Id, order);

        Assert.Throws<ValidationException>(() => service.CompleteOrder(patient.Id, order.Id, doctor.Id));
        service.CompleteOrder(patient.Id, order.Id, nurse.Id);

        Assert.Equal(OrderStatus.Completed, service.GetPatient(patient.Id).Orders.Single().Status);
    }

    [Fact]
    public void Discharge_IsBlockedWhileOrdersRemainIncomplete()
    {
        var service = CreateService(out _, out _);
        var patient = service.Admit(BusinessValidationTests.CreateValidPatient());
        service.AddOrder(patient.Id, new MedicalOrder { Type = OrderType.Diagnostic, Name = "УЗИ" });

        Assert.Throws<ValidationException>(() => service.Discharge(patient.Id));
        Assert.False(service.GetPatient(patient.Id).IsDischarged);
    }

    [Fact]
    public void DischargeSummary_ContainsPatientDiagnosisAndCompletedOrder()
    {
        var service = CreateService(out var doctor, out _);
        var patient = service.Admit(BusinessValidationTests.CreateValidPatient());
        service.AddOrder(patient.Id, new MedicalOrder { Type = OrderType.Diagnostic, Name = "УЗИ" });
        service.CompleteOrder(patient.Id, patient.Orders.Single().Id, doctor.Id);
        service.Discharge(patient.Id);

        var summary = service.CreateDischargeSummary(patient.Id);

        Assert.Contains(patient.FullName, summary);
        Assert.Contains(patient.AdmissionDiagnosis, summary);
        Assert.Contains("УЗИ", summary);
        Assert.Contains(doctor.FullName, summary);
    }

    private static HospitalService CreateService(out StaffMember doctor, out StaffMember nurse)
    {
        doctor = new StaffMember { FullName = "Смирнов Алексей Викторович", Role = StaffRole.Doctor, Department = "Кардиологическое" };
        nurse = new StaffMember { FullName = "Петрова Анна Игоревна", Role = StaffRole.Nurse, Department = "Кардиологическое" };
        var data = new HospitalData { Staff = [doctor, nurse] };
        return new HospitalService(new InMemoryHospitalRepository(data));
    }

    private sealed class InMemoryHospitalRepository(HospitalData data) : IHospitalRepository
    {
        private HospitalData _data = data;
        public HospitalData Load() => _data;
        public void Save(HospitalData data) => _data = data;
    }
}

