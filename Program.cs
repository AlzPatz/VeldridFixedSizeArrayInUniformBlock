using System;
using System.IO;

namespace arrayuniform
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Attempting to correctly use an array within a Uniform Block in Open GL");

            var demo = new Demo();

            demo.Run();
        }
    }
}