using Hospital.Domain;
using Hospital.Services;

namespace Hospital.WinForms;

public sealed class MainForm : Form
{
    private readonly HospitalService _service;
    private readonly DataGridView _patients = Grid();
    private readonly DataGridView _orders = Grid();
    private readonly DataGridView _staff = Grid();
    private readonly Label _recordInfo = new() { AutoSize = true, Padding = new Padding(8) };
    private Guid? SelectedPatientId => _patients.CurrentRow?.DataBoundItem is Patient p ? p.Id : null;

    public MainForm(HospitalService service)
    {
        _service = service;
        Text = "Больница — система управления лечением"; Width = 1200; Height = 760; MinimumSize = new Size(950, 600); StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f); BuildUi(); RefreshAll();
    }
    private void BuildUi()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };
        var patientTab = new TabPage("Пациенты и ЭМК");
        var staffTab = new TabPage("Персонал и отделения");
        tabs.TabPages.Add(patientTab); tabs.TabPages.Add(staffTab); Controls.Add(tabs);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 48)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
        patientTab.Controls.Add(layout);
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        toolbar.Controls.Add(Button("Госпитализировать", (_,_) => AdmitPatient()));
        toolbar.Controls.Add(Button("Назначить лечение", (_,_) => AddOrder()));
        toolbar.Controls.Add(Button("Отметить выполнение", (_,_) => CompleteOrder()));
        toolbar.Controls.Add(Button("Выписать и сформировать эпикриз", (_,_) => Discharge()));
        toolbar.Controls.Add(Button("Обновить", (_,_) => RefreshAll()));
        layout.Controls.Add(toolbar,0,0);
        _patients.SelectionChanged += (_,_) => RefreshSelectedPatient();
        _patients.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        layout.Controls.Add(_patients,0,1);
        var recordPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        recordPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); recordPanel.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        _recordInfo.Font = new Font(Font, FontStyle.Bold); recordPanel.Controls.Add(_recordInfo,0,0); recordPanel.Controls.Add(_orders,0,1); layout.Controls.Add(recordPanel,0,2);

        var staffLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(12) };
        staffLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); staffLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100)); staffTab.Controls.Add(staffLayout);
        staffLayout.Controls.Add(new Label { Dock=DockStyle.Fill, Text="Сотрудники и отделения. Медикаментозные назначения выполняют медсестры; остальные — врачи.", TextAlign=ContentAlignment.MiddleLeft },0,0);
        staffLayout.Controls.Add(_staff,0,1);
    }
    private static DataGridView Grid() => new() { Dock=DockStyle.Fill, ReadOnly=true, AllowUserToAddRows=false, AllowUserToDeleteRows=false, AutoGenerateColumns=false, AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible=false, BackgroundColor=Color.White, MultiSelect=false, SelectionMode=DataGridViewSelectionMode.FullRowSelect };
    private static Button Button(string text, EventHandler action) { var b=new Button { Text=text, AutoSize=true, Height=36, Margin=new Padding(3,5,8,4) }; b.Click+=action; return b; }
    private void RefreshAll()
    {
        var selected=SelectedPatientId;
        _patients.Columns.Clear();
        AddCol(_patients,"FullName","Пациент", "FullName"); AddCol(_patients,"MedicalRecordNumber","№ ЭМК", "MedicalRecordNumber"); AddCol(_patients,"AdmittedAt","Поступил", "AdmittedAt", "dd.MM.yyyy HH:mm"); AddCol(_patients,"Department","Отделение", "Department"); AddCol(_patients,"AdmissionDiagnosis","Диагноз при поступлении", "AdmissionDiagnosis"); AddCol(_patients,"AttendingDoctor","Лечащий врач", "AttendingDoctor");
        var source=_service.Patients.ToList(); _patients.DataSource=source;
        _staff.Columns.Clear(); AddCol(_staff,"FullName","ФИО", "FullName"); AddCol(_staff,"RoleName","Должность", "RoleName"); AddCol(_staff,"Department","Отделение", "Department");
        _staff.DataSource=_service.Doctors.Concat(_service.Nurses).Select(s=>new {s.FullName,RoleName=RoleText(s.Role),s.Department}).ToList();
        if(selected.HasValue) foreach(DataGridViewRow row in _patients.Rows) if(row.DataBoundItem is Patient p && p.Id==selected) { row.Selected=true; break; }
        RefreshSelectedPatient();
    }
    private void RefreshSelectedPatient()
    {
        var p=SelectedPatientId.HasValue ? _service.GetPatient(SelectedPatientId.Value) : null;
        if(p is null) { _recordInfo.Text="Выберите пациента, чтобы открыть ЭМК"; _orders.DataSource=null; return; }
        _recordInfo.Text=$"ЭМК {p.MedicalRecordNumber}  |  {p.FullName}  |  {(p.IsDischarged ? "ВЫПИСАН " + p.DischargedAt?.ToString("dd.MM.yyyy HH:mm") : "На лечении")}  |  {p.BirthDate:dd.MM.yyyy}, {p.Gender}, {p.Phone}";
        _orders.Columns.Clear(); AddCol(_orders,"Name","Назначение", "Name"); AddCol(_orders,"OrderTypeName","Тип", "OrderTypeName"); AddCol(_orders,"Description","Параметры", "Description"); AddCol(_orders,"CreatedAt","Назначено", "CreatedAt", "dd.MM.yyyy HH:mm"); AddCol(_orders,"CompletedAt","Выполнено", "CompletedAt", "dd.MM.yyyy HH:mm"); AddCol(_orders,"PerformedBy","Кем выполнено", "PerformedBy");
        _orders.DataSource=p.Orders.Select(o=>new {o.Id,o.Name,OrderTypeName=HospitalService.TypeName(o.Type),Description=HospitalService.FormatDetails(o),o.CreatedAt,CompletedAt=o.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Ожидает",o.PerformedBy}).ToList();
    }
    private static void AddCol(DataGridView grid,string name,string title,string property,string? format=null) { var c=new DataGridViewTextBoxColumn {Name=name,HeaderText=title,DataPropertyName=property,SortMode=DataGridViewColumnSortMode.Automatic}; if(format!=null)c.DefaultCellStyle.Format=format; grid.Columns.Add(c); }
    private void AdmitPatient()
    {
        using var f=new AdmissionForm(_service);
        if(f.ShowDialog(this)!=DialogResult.OK)return;
        try { _service.Admit(f.Patient); RefreshAll(); MessageBox.Show(this,$"Пациент госпитализирован. Создана ЭМК {f.Patient.MedicalRecordNumber}.","Готово"); }
        catch(Exception ex){ ShowError(ex); }
    }
    private void AddOrder()
    {
        if(!RequireActivePatient(out var id))return;
        using var f=new OrderForm(); if(f.ShowDialog(this)!=DialogResult.OK)return;
        try { _service.AddOrder(id,f.Order); RefreshAll(); } catch(Exception ex){ShowError(ex);}
    }
    private void CompleteOrder()
    {
        if(!RequireActivePatient(out var id))return;
        if(_orders.CurrentRow?.DataBoundItem is not { } row) { MessageBox.Show(this,"Выберите назначение в ЭМК.");return; }
        var orderId=(Guid)row.GetType().GetProperty("Id")!.GetValue(row)!;
        var order=_service.GetPatient(id).Orders.Single(x=>x.Id==orderId);
        var eligible=order.Type is OrderType.Injection or OrderType.Tablets ? _service.Nurses : _service.Doctors;
        using var f=new WorkerForm(eligible,order.Type is OrderType.Injection or OrderType.Tablets ? "Выберите медсестру" : "Выберите врача");
        if(f.ShowDialog(this)!=DialogResult.OK || f.Worker is null)return;
        try { _service.CompleteOrder(id,orderId,f.Worker.Id); RefreshAll(); } catch(Exception ex){ShowError(ex);}
    }
    private void Discharge()
    {
        if(!RequireActivePatient(out var id))return;
        try { _service.Discharge(id); RefreshAll(); var summary=_service.CreateDischargeSummary(id); using var f=new SummaryForm(summary); f.ShowDialog(this); }
        catch(Exception ex){ShowError(ex);}
    }
    private bool RequireActivePatient(out Guid id)
    {
        id=SelectedPatientId??Guid.Empty; if(id==Guid.Empty){MessageBox.Show(this,"Сначала выберите пациента.");return false;}
        if(_service.GetPatient(id).IsDischarged){MessageBox.Show(this,"Пациент уже выписан.");return false;} return true;
    }
    private void ShowError(Exception ex)=>MessageBox.Show(this,ex.Message,"Проверка данных",MessageBoxButtons.OK,MessageBoxIcon.Warning);
    internal static string RoleText(StaffRole r)=>r switch {StaffRole.DutyDoctor=>"Дежурный врач",StaffRole.Doctor=>"Врач",_=>"Медсестра"};
}

internal sealed class AdmissionForm : Form
{
    private readonly TextBox _name=Field(),_phone=Field(),_address=Field(),_diagnosis=Field();
    private readonly DateTimePicker _birth=new(){Format=DateTimePickerFormat.Short,MaxDate=DateTime.Today,Value=DateTime.Today.AddYears(-30)};
    private readonly ComboBox _gender=Combo("Женский","Мужской","Другой"),_department=new(){DropDownStyle=ComboBoxStyle.DropDownList},_doctor=new(){DropDownStyle=ComboBoxStyle.DropDownList};
    public Patient Patient {get;private set;}=new();
    public AdmissionForm(HospitalService service)
    {
        Text="Госпитализация и первичный осмотр"; Width=560; Height=570; StartPosition=FormStartPosition.CenterParent; FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false;
        _gender.SelectedIndex=0; _department.Items.AddRange(service.Departments.Cast<object>().ToArray()); _doctor.DisplayMember="FullName";
        _department.SelectedIndexChanged+=(_,_)=>{var eligible=service.Doctors.Where(d=>d.Role==StaffRole.DutyDoctor || d.Department==_department.Text).ToArray();_doctor.Items.Clear();_doctor.Items.AddRange(eligible.Cast<object>().ToArray());if(_doctor.Items.Count>0)_doctor.SelectedIndex=0;};
        if(_department.Items.Count>0)_department.SelectedIndex=0;
        var rows=new (string,Control)[]{("ФИО",_name),("Дата рождения",_birth),("Пол",_gender),("Телефон",_phone),("Адрес",_address),("Диагноз при поступлении",_diagnosis),("Отделение",_department),("Лечащий врач",_doctor)};
        var panel=FormHelpers.FormPanel(rows); Controls.Add(panel); FormHelpers.AddSave(panel,()=>
        {
            if(_doctor.SelectedItem is not StaffMember d)throw new ValidationException("Добавьте врача в справочник персонала.");
            Patient=new Patient{FullName=_name.Text.Trim(),BirthDate=_birth.Value.Date,Gender=_gender.Text,Phone=_phone.Text.Trim(),Address=_address.Text.Trim(),AdmissionDiagnosis=_diagnosis.Text.Trim(),Department=_department.Text,AttendingDoctorId=d.Id,AttendingDoctor=d.FullName}; Patient.Validate();
        });
    }
    private static TextBox Field()=>new(){Width=330};
    private static ComboBox Combo(params string[] items){var c=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=330};c.Items.AddRange(items);return c;}
}

internal sealed class OrderForm : Form
{
    private readonly ComboBox _type=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=330}; private readonly TextBox _name=new(){Width=330},_details=new(){Width=330};
    private readonly NumericUpDown _quantity=new(){Minimum=1,Maximum=10000,Value=1},_dosage=new(){Minimum=0.01m,Maximum=100000,DecimalPlaces=2,Increment=0.5m,Value=1},_days=new(){Minimum=1,Maximum=365,Value=1};
    public MedicalOrder Order {get;private set;}=new();
    public OrderForm()
    {
        Text="Новое назначение врача"; Width=560; Height=480; StartPosition=FormStartPosition.CenterParent; FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false;
        _type.Items.AddRange(new object[]{"Инъекция","Таблетки","Диагностическое","Профилактическое"}); _type.SelectedIndex=0;
        var panel=FormHelpers.FormPanel(new (string,Control)[]{("Тип назначения",_type),("Препарат / процедура",_name),("Количество инъекций",_quantity),("Дозировка таблеток",_dosage),("Дней приема",_days),("Дополнительные сведения",_details)}); Controls.Add(panel);
        _type.SelectedIndexChanged+=(_,_)=>UpdateFields(); UpdateFields();
        FormHelpers.AddSave(panel,()=>{ Order=new MedicalOrder{Type=(OrderType)_type.SelectedIndex,Name=_name.Text.Trim(),Quantity=(int)_quantity.Value,Dosage=_dosage.Value,Days=(int)_days.Value,Details=_details.Text.Trim()}; Order.Validate(); });
    }
    private void UpdateFields(){_quantity.Enabled=_type.SelectedIndex==0;_dosage.Enabled=_type.SelectedIndex==1;_days.Enabled=_type.SelectedIndex==1;_details.Enabled=_type.SelectedIndex>=2;}
}

internal sealed class WorkerForm : Form
{
    private readonly ComboBox _worker=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=330}; public StaffMember? Worker=>_worker.SelectedItem as StaffMember;
    public WorkerForm(IReadOnlyList<StaffMember> workers,string title)
    {
        Text=title; Width=490; Height=180; StartPosition=FormStartPosition.CenterParent; _worker.Items.AddRange(workers.Cast<object>().ToArray()); _worker.DisplayMember="FullName"; if(_worker.Items.Count>0)_worker.SelectedIndex=0;
        var panel=FormHelpers.FormPanel(new (string,Control)[]{("Исполнитель",_worker)}); Controls.Add(panel); FormHelpers.AddSave(panel,()=>{if(Worker is null)throw new ValidationException("Нет сотрудника с подходящей должностью.");});
    }
}

internal sealed class SummaryForm : Form
{
    public SummaryForm(string text)
    {
        Text="Выписной эпикриз"; Width=760; Height=650; StartPosition=FormStartPosition.CenterParent;
        var box=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Font=new Font("Segoe UI",10),Text=text}; var save=new Button{Text="Сохранить эпикриз в файл...",Dock=DockStyle.Bottom,Height=40};
        save.Click+=(_,_)=>{using var dialog=new SaveFileDialog{Filter="Текстовый файл (*.txt)|*.txt",FileName="Выписной_эпикриз.txt"};if(dialog.ShowDialog(this)==DialogResult.OK)File.WriteAllText(dialog.FileName,box.Text);}; Controls.Add(box);Controls.Add(save);
    }
}

internal static class FormHelpers
{
    public static TableLayoutPanel FormPanel((string label,Control control)[] rows)
    {
        var p=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=2,RowCount=rows.Length+1,AutoScroll=true};p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,180));p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        for(int i=0;i<rows.Length;i++){p.RowStyles.Add(new RowStyle(SizeType.Absolute,46));p.Controls.Add(new Label{Text=rows[i].label,TextAlign=ContentAlignment.MiddleLeft,Dock=DockStyle.Fill},0,i);rows[i].control.Dock=DockStyle.Left;p.Controls.Add(rows[i].control,1,i);}
        p.RowStyles.Add(new RowStyle(SizeType.Absolute,54));return p;
    }
    public static void AddSave(TableLayoutPanel panel,Action validate)
    {
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};var ok=new Button{Text="Сохранить",Width=120,Height=34};var cancel=new Button{Text="Отмена",Width=100,Height=34,DialogResult=DialogResult.Cancel};
        ok.Click+=(_,_)=>{try{validate();panel.FindForm()!.DialogResult=DialogResult.OK;panel.FindForm()!.Close();}catch(Exception ex){MessageBox.Show(panel.FindForm(),ex.Message,"Проверка данных",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};
        buttons.Controls.Add(ok);buttons.Controls.Add(cancel);panel.Controls.Add(buttons,1,panel.RowCount-1);panel.FindForm()!.AcceptButton=ok;panel.FindForm()!.CancelButton=cancel;
    }
}