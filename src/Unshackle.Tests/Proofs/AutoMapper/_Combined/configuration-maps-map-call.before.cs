using AutoMapper;

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
            cfg.CreateMap<Department, DepartmentDto>().ReverseMap();
            cfg.CreateMap<Doctor, DoctorDto>()
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department.Name));
        });

        public static Mapper GetMapper()
        {
            return new Mapper(config);
        }
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

        public Mapping.DoctorDto Get(Mapping.Doctor doctor) => _mapper.Map<Mapping.DoctorDto>(doctor);

        public Mapping.Department Back(Mapping.DepartmentDto dto) => _mapper.Map<Mapping.Department>(dto);
    }
}
