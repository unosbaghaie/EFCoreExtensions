using EFCoreExtensions.Models;
using System;
using System.Linq;

namespace EFCoreExtensions.Console
{
    class Program
    {
        static void Main(string[] args)
        {



            System.Console.WriteLine("Hello World!");


            var db = new TestContext();



            var query = (from u in db.Set<User>()
                         join p in db.Set<Product>() on u.Id equals p.UserId
                         select u);

            var queryResult = query.SelectWithCommand2();


            var lst3 = db.Set<User>().Where(q => q.Id > 2 && q.Name.Contains("3"))
            .Select(q => new User { Age = q.Age, Name = q.Name }).IQueryableToSql();





            var lst2 = db.Set<User>().Select3(q => new User { Age = q.Age, Name = q.Name }, q => q.Id > 2 && q.Name.Contains("3"));

            var lst = db.Set<User>().Where(q => q.Id > 2 && q.Name.Contains("3")).
                SelectWithCommand(q => new User { Age = q.Age, Name = q.Name }, q => q.Id > 2 && q.Name.Contains("3"));


        }
    }
}
