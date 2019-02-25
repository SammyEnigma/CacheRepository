using System;
using System.Collections.Generic;
using System.Text;

namespace CacheRepository.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var e = new ExprVisitor();
            e.Test();
            Console.Read();
        }
    }
}
