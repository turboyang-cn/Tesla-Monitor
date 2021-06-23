
using AutoMapper;

namespace TurboYang.Tesla.Monitor.Mapping
{
    public abstract class MappingProfile : Profile
    {
        protected abstract void Mapping();

        public MappingProfile()
        {
            Mapping();
        }
    }
}
