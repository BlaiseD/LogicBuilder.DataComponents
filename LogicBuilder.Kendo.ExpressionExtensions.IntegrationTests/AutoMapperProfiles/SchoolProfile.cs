﻿using AutoMapper;
using Contoso.Data.Entities;
using Contoso.Domain.Entities;

namespace LogicBuilder.Kendo.ExpressionExtensions.IntegrationTests.AutoMapperProfiles
{
    public class SchoolProfile : Profile
    {
        public SchoolProfile()
        {
            CreateMap<CourseAssignmentModel, CourseAssignment>()
                .ForMember(dest => dest.Instructor, opts => opts.Ignore())
                .ForMember(dest => dest.Course, opts => opts.Ignore())
                .ReverseMap()
                .ForMember(dest => dest.CourseTitle, opts => opts.MapFrom(x => x.Course.Title))
                .ForMember(dest => dest.CourseNumberAndTitle, opts => opts.MapFrom(x => x.Course.CourseID.ToString() + " " + x.Course.Title))
                .ForMember(dest => dest.Department, opts => opts.MapFrom(x => x.Course.Department.Name))
                .ForAllMembers(o => o.ExplicitExpansion());

            CreateMap<CourseModel, Course>()
                .ForMember(dest => dest.Department, opts => opts.Ignore())
                .ForMember(dest => dest.Enrollments, opts => opts.Ignore())
                .ReverseMap()
                .ForMember(dest => dest.DepartmentName, opts => opts.MapFrom(x => x.Department.Name))
                .ForAllMembers(o => o.ExplicitExpansion());

            CreateMap<DepartmentModel, Department>()
                .ForMember(dest => dest.Administrator, opts => opts.Ignore())
                .ReverseMap()
                .ForMember(dest => dest.AdministratorName, opts => opts.MapFrom(x => x.Administrator.FirstName + " " + x.Administrator.LastName))
                .ForAllMembers(o => o.ExplicitExpansion());

            CreateMap<EnrollmentModel, Enrollment>()
                .ForMember(dest => dest.Student, opts => opts.Ignore())
                .ForMember(dest => dest.Course, opts => opts.Ignore())
                .ReverseMap()
                .ForMember(dest => dest.CourseTitle, opts => opts.MapFrom(x => x.Course.Title))
                .ForMember(dest => dest.StudentName, opts => opts.MapFrom(x => x.Student.FirstName + " " + x.Student.LastName))
                .ForMember(dest => dest.Grade, opts => opts.MapFrom(x => x.Grade.HasValue ? (Contoso.Domain.Entities.Grade?)(int)x.Grade.Value : null))
                .ForMember
                (
                    dest => dest.GradeLetter,
                    opts => opts.MapFrom
                    (
                        x => x.Grade == Contoso.Data.Entities.Grade.A ? "A"
                            : x.Grade == Contoso.Data.Entities.Grade.B ? "B"
                            : x.Grade == Contoso.Data.Entities.Grade.C ? "C"
                            : x.Grade == Contoso.Data.Entities.Grade.D ? "D"
                            : x.Grade == Contoso.Data.Entities.Grade.F ? "F" : ""
                    )
                )
                .ForAllMembers(o => o.ExplicitExpansion());

            CreateMap<InstructorModel, Instructor>()
                .ReverseMap()
                .ForMember(dest => dest.FullName, opts => opts.MapFrom(x => x.FirstName + " " + x.LastName))
                .ForAllMembers(o => o.ExplicitExpansion());

            CreateMap<OfficeAssignmentModel, OfficeAssignment>()
                .ForMember(dest => dest.Instructor, opts => opts.Ignore())
                .ReverseMap()
                .ForAllMembers(o => o.ExplicitExpansion());

            CreateMap<StudentModel, Student>()
                .ReverseMap()
            .ForMember(dest => dest.FullName, opts => opts.MapFrom(x => x.FirstName + " " + x.LastName))
            .ForAllMembers(o => o.ExplicitExpansion());
        }
    }
}
