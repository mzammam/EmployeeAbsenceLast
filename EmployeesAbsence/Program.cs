using System;
using AbscenceSystem;

namespace EmployeesAbsence
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            IFileHandler fHandler = new FileHandler();
            fHandler.ReadCsvFile("C:\\Users\\mzavqt\\Downloads\\StartData.csv");
            fHandler.ReadXmlFiles("C:\\Users\\mzavqt\\Downloads\\AbsenceXmls");
            fHandler.ScanAndUpdate();
            fHandler.WriteNewCsvToFile();
        }
    }
}
