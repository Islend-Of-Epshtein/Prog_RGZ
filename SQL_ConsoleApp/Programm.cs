using System;
using SQL_ConsoleApp.CLI;

namespace SQL_ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "SQL Interpreter";
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
            ConsoleInterface.Run();
        }
    }
}