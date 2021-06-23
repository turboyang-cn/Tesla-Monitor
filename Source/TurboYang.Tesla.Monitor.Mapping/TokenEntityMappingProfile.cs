using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TurboYang.Tesla.Monitor.Database.Entities;
using TurboYang.Tesla.Monitor.Model;

namespace TurboYang.Tesla.Monitor.Mapping
{
    public class TokenEntityMappingProfile : MappingProfile
    {
        protected override void Mapping()
        {
            CreateMap<TokenEntity, Token>()
                .ForMember(x => x.OpenId, x => x.MapFrom(x => x.OpenId))
                .ForMember(x => x.Username, x => x.MapFrom(x => x.Username))
                .ForMember(x => x.CreateBy, x => x.MapFrom(x => x.CreateBy))
                .ForMember(x => x.UpdateBy, x => x.MapFrom(x => x.UpdateBy))
                .ForMember(x => x.CreateTimestamp, x => x.MapFrom(x => x.CreateTimestamp))
                .ForMember(x => x.UpdateTimestamp, x => x.MapFrom(x => x.UpdateTimestamp));
        }
    }
}
