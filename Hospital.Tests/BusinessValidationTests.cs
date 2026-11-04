using Xunit;
using Hospital.Domain;

namespace Hospital.Tests;

public sealed class BusinessValidationTests
{
    [Fact]
    public void Patient_WithRequiredAdmissionData_IsValid()
    {
        var patient = CreateValidPatient();

        patient.Validate();

        Assert.NotEqual(Guid.Empty, patient.Id);
    }

    [Fact]
    public void Patient_WithoutName_IsRejected()
    {
        var patient = CreateValidPatient();
        patient.FullName = " ";

        Assert.Throws<ValidationException>(() => patient.Validate());
    }

    [Fact]
    public void Patient_WithFutureBirthDate_IsRejected()
    {
        var patient = CreateValidPatient();
        patient.BirthDate = DateTime.Today.AddDays(1);

        Assert.Throws<ValidationException>(() => patient.Validate());
    }

    [Fact]
    public void Injection_WithoutPositiveQuantity_IsRejected()
    {
        var order = new MedicalOrder { Type = OrderType.Injection, Name = "Антибиотик", Quantity = 0 };

        Assert.Throws<ValidationException>(() => order.Validate());
    }

    [Fact]
    public void Tablets_WithoutDosageAndDuration_AreRejected()
    {
        var order = new MedicalOrder { Type = OrderType.Tablets, Name = "Препарат" };

        Assert.Throws<ValidationException>(() => order.Validate());
    }

    [Fact]
    public void Tablets_WithDosageAndDuration_AreValid()
    {
        var order = new MedicalOrder { Type = OrderType.Tablets, Name = "Препарат", Dosage = 2.5m, Days = 7 };

        order.Validate();

        Assert.Equal(OrderStatus.Planned, order.Status);
    }

    internal static Patient CreateValidPatient() => new()
    {
        FullName = "Иванов Иван Иванович",
        BirthDate = new DateTime(1990, 1, 1),
        Gender = "Мужской",
        AdmissionDiagnosis = "Плановое наблюдение",
        Department = "Кардиологическое",
        AttendingDoctor = "Смирнов Алексей Викторович"
    };
}

