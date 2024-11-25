using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting.Dependencies.Sqlite;
using Supabase;
using Postgrest;
using Postgrest.Models;

namespace Assets
{
    [Postgrest.Attributes.Table("users")]
    public class Users : BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public int id { get; set; }

        [Postgrest.Attributes.Column("login")]
        public string? login { get; set; }

        [Postgrest.Attributes.Column("password")]
        public string? password { get; set; }
    }
}
