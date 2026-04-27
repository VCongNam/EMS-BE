using EMS.Application.Features.Classes.DTOs;
using EMS.Application.Features.Classes.Validators;
using FluentValidation.TestHelper;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Tests.Features.Classes
{
    [TestFixture]
    public class CreateClassDtoValidatorTests
    {
        private CreateClassDtoValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new CreateClassDtoValidator();
        }

        [Test]
        public void ClassName_Empty_ShouldHaveError()
        {
            // 1. Arrange
            var request = new CreateClassDto { ClassName = "" };

            // 2. Act
            var result = _validator.TestValidate(request);

            // 3. Assert (Cú pháp của TestHelper)
            result.ShouldHaveValidationErrorFor(x => x.ClassName)
                  .WithErrorMessage("Tên lớp không được để trống.");
        }

        [Test]
        public void TuitionFee_Negative_ShouldHaveError()
        {
            var request = new CreateClassDto { TuitionFee = -500000 };
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.TuitionFee)
                  .WithErrorMessage("Học phí không được âm.");
        }

        [Test]
        public void EndDate_Before_StartDate_ShouldHaveError()
        {
            var request = new CreateClassDto
            {
                StartDate = new DateOnly(2024, 5, 1),
                EndDate = new DateOnly(2024, 4, 1) // Kết thúc trước khi bắt đầu
            };
            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.EndDate)
                  .WithErrorMessage("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
        }

        [Test]
        public void Request_ValidData_ShouldNotHaveAnyError()
        {
            var request = new CreateClassDto
            {
                ClassName = "Lớp Toán",
                SubjectName = "Toán",
                GradeLevel = 10,
                MaxStudents = 20,
                TuitionFee = 1000000,
                StartDate = new DateOnly(2024, 5, 1),
                EndDate = new DateOnly(2024, 8, 1),
                BillingMethod = "Prepaid"
            };

            var result = _validator.TestValidate(request);

            // Mong đợi không có bất kỳ lỗi nào xuất hiện
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
