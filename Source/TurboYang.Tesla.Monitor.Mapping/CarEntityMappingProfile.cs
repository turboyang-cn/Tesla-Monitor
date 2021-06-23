using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TurboYang.Tesla.Monitor.Database.Entities;
using TurboYang.Tesla.Monitor.Model;

namespace TurboYang.Tesla.Monitor.Mapping
{
    public class CarEntityMappingProfile : MappingProfile
    {
        protected override void Mapping()
        {
            CreateMap<CarEntity, Car>()
                .ForMember(x => x.OpenId, x => x.MapFrom(x => x.OpenId))
                .ForMember(x => x.CarId, x => x.MapFrom(x => x.CarId))
                .ForMember(x => x.VehicleId, x => x.MapFrom(x => x.VehicleId))
                .ForMember(x => x.Name, x => x.MapFrom(x => x.Name))
                .ForMember(x => x.Type, x => x.MapFrom(x => x.Type))
                .ForMember(x => x.Vin, x => x.MapFrom(x => x.Vin))
                .ForMember(x => x.ExteriorColor, x => x.MapFrom(x => x.ExteriorColor))
                .ForMember(x => x.WheelType, x => x.MapFrom(x => x.WheelType))
                .ForMember(x => x.CreateBy, x => x.MapFrom(x => x.CreateBy))
                .ForMember(x => x.UpdateBy, x => x.MapFrom(x => x.UpdateBy))
                .ForMember(x => x.CreateTimestamp, x => x.MapFrom(x => x.CreateTimestamp))
                .ForMember(x => x.UpdateTimestamp, x => x.MapFrom(x => x.UpdateTimestamp));
        }
    }
}
