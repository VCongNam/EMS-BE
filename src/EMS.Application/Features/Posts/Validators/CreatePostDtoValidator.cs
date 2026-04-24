using EMS.Application.Features.Posts.DTOs;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Posts.Validators
{
    public class CreatePostDtoValidator : AbstractValidator<CreatePostDto>
    {
        public CreatePostDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề không được để trống.")
                .MaximumLength(200).WithMessage("Tiêu đề không quá 200 ký tự.");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Nội dung không được để trống.");

            RuleFor(x => x.ClassId)
                .NotEmpty().WithMessage("Mã lớp học là bắt buộc.");
        }
    }
}
