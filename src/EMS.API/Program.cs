using EMS.Application.Features.Classes.Services;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Data;
using EMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using EMS.Application.Features.Assignments.Services;

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Đăng ký Repository (Domain <-> Infra)
builder.Services.AddScoped<IClassRepository, ClassRepository>();

// 3. Đăng ký Service (Application)
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<IAssignmentRepository, AssignmentRepository>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
