using System.Collections.Generic;
using System.Linq;
using AutoMapper;

namespace Devices
{
    public class ProduceData
    {
        public string Key { get; set; } = "";
    }

    public class AttributeLatest
    {
        public string Key { get; set; } = "";
    }

    public class DevicesController
    {
        public List<AttributeLatest> Attributes(List<ProduceData> defaults)
        {
            MapperConfiguration mapperConfiguration = new MapperConfiguration(options => { options.CreateMap<ProduceData, AttributeLatest>(); });
            IMapper mapper = mapperConfiguration.CreateMapper();
            return defaults.Select(c => mapper.Map<ProduceData, AttributeLatest>(c)).ToList();
        }
    }
}
