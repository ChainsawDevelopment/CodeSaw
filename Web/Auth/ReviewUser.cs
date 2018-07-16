﻿namespace Web.Auth
{
    public class ReviewUser
    {
        public virtual int Id { get; set; }

        public virtual string UserName { get; set; }

        public virtual string Token { get; set; }
        
        public virtual string GivenName { get; set; }

        public virtual string AvatarUrl { get; set; }
    }
}