using System;

namespace WebApplication.Models
{
    public class User
    {
        public string _id { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string createdDate { get; set; }
        public string modifiedDate { get; set; }

    }
}