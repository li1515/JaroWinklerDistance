﻿//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace EXON.Penny_JaroWinklerDistance
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class AdresyCZEntities2 : DbContext
    {
        public AdresyCZEntities2()
            : base("name=AdresyCZEntities2")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public DbSet<castObce> castObces { get; set; }
        public DbSet<obce> obces { get; set; }
        public DbSet<psc> pscs { get; set; }
        public DbSet<allrec> allrecs { get; set; }
    }
}
