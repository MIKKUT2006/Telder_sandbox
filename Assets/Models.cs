using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting.Dependencies.Sqlite;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Assets
{
    [Supabase.Postgrest.Attributes.Table("users")]
    public class Users : BaseModel
    {
        [Supabase.Postgrest.Attributes.PrimaryKey("id", false)]
        public int id { get; set; }

        [Supabase.Postgrest.Attributes.Column("login")]
        public string? login { get; set; }

        [Supabase.Postgrest.Attributes.Column("password")]
        public string? password { get; set; }
    }
}
