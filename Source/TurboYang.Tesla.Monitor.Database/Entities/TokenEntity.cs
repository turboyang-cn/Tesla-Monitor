using System;

namespace TurboYang.Tesla.Monitor.Database.Entities
{
    public class TokenEntity : BaseEntity
    {
        public String Username { get; set; }
        public String AccessToken { get; set; }
        public String RefreshToken { get; set; }
    }
}
