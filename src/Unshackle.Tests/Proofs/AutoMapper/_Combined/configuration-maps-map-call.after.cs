using AutoMapper;
using Shop.Mapping;

namespace Shop.Mapping
{
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class DepartmentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class Doctor
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public Department Department { get; set; } = new();
    }

    public class DoctorDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string DepartmentName { get; set; } = "";
    }

    public static class MapperConfig
    {
        private static readonly MapperConfiguration config = new(cfg =>
        {
        });

        public static Mapper GetMapper()
        {
            return new Mapper(config);
        }
    }

    public static class MapperConfigExtensions
    {
        public static DepartmentDto ToDepartmentDto(this Department source) => new()
        {
            Id = source.Id,
            Name = source.Name,
        };

        public static Department ToDepartment(this DepartmentDto source) => new()
        {
            Id = source.Id,
            Name = source.Name,
        };

        public static DoctorDto ToDoctorDto(this Doctor source) => new()
        {
            Id = source.Id,
            Name = source.Name,
            DepartmentName = source.Department?.Name,
        };
    }
}

namespace Shop.Services
{
    public class DoctorService
    {
        private readonly Mapper _mapper;

        public DoctorService()
        {
            _mapper = Mapping.MapperConfig.GetMapper();
        }

        public Mapping.DoctorDto Get(Mapping.Doctor doctor) => doctor.ToDoctorDto();

        public Mapping.Department Back(Mapping.DepartmentDto dto) => dto.ToDepartment();
    }
}
