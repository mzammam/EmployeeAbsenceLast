using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Collections.Specialized;

namespace AbscenceSystem
{
    public class FileHandler : IFileHandler
    {
        Dictionary<int, List<Absence>> EmpDictionaryCsv = new Dictionary<int, List<Absence>>();

        SortedDictionary<DateTime, List<Employee>> EmpDictionaryXml = new SortedDictionary<DateTime, List<Employee>>();
        //OrderedDictionary EmpDictionaryXml = new OrderedDictionary();

        private List<Employee> result = new List<Employee>();

        public FileHandler()
        {

        }

        #region csv


        public void ReadCsvFile(string csvfile)
        {
            if (!File.Exists(csvfile)) throw new Exception($"File {csvfile} doesn't exist!");
            /*var employees = new List<Employee>();
*/
            var currentAbsence = ReadCsv(csvfile);
            HandleAbsenceDetails(currentAbsence);
            /*return employees;*/
        }

        private void HandleAbsenceDetails(List<KeyValuePair<int, Absence>> absences)
        {

            var absenceDetails = absences
                .Select(x => x.Key)
                .Distinct()
                .Select(item => new
                {
                    id = item,
                    absence = absences.Where(i => i.Key == item).Select(i => i.Value).ToList()
                });
            foreach (var embAbDetail in absenceDetails)
            {
                foreach (var absenceRecord in embAbDetail.absence)
                {
                    AddNewAbsenceRec(absenceRecord, embAbDetail.id);

                }

            }
        }
        
        private void AddNewAbsenceRec(Absence absRec, int id)
        {
            var l = new List<Absence>();
            if (EmpDictionaryCsv.ContainsKey(id))
            {
                l = EmpDictionaryCsv[id];
                l.Add(absRec);
                return;
            }

            l.Add(absRec);
            EmpDictionaryCsv.Add(id, l);
        }

        private void HandleAbscenceDetails2(List<KeyValuePair<int, Absence>> absences)
        {
            DateTime start;
            DateTime last;
            Absence current;
            var absenceDetails = absences
                .Select(x => x.Key)
                .Distinct()
                .Select(item => new
                {
                    id = item,
                    absence = absences.Where(i => i.Key == item).Select(i => i.Value).ToList()
                });
            foreach (var embAbDetail in absenceDetails)
            {
                start = GetStartDate(embAbDetail.absence);
                last = start;
                current = embAbDetail.absence.First(); // current = 1;2019-03-01;1;0,85
                foreach (var abscenceRecord in embAbDetail.absence.Skip(1))
                {
                    if (GetDiffInDays(abscenceRecord.StartDate, last) > 1)
                    {
                        //A new absence
                        AddNewAbsenceRec2(current, last, embAbDetail.id);
                        current = abscenceRecord;
                        last = abscenceRecord.StartDate;

                    }
                    else
                    {
                        last = abscenceRecord.StartDate; //last = 2019-03-02,  start = 2019-03-01
                        if (current.Percentage != abscenceRecord.Percentage)
                        {
                            AddNewAbsenceRec2(current, last, embAbDetail.id);
                            current = abscenceRecord;
                            last = abscenceRecord.StartDate;
                        }
                    }
                }

                AddNewAbsenceRec2(current, last, embAbDetail.id);
            }
        }

        private void AddNewAbsenceRec2(Absence absRec, DateTime endDate, int id)
        {
            absRec.EndDate = endDate;
            var l = new List<Absence>();
            if (EmpDictionaryCsv.ContainsKey(id))
            {
                l = EmpDictionaryCsv[id];
                l.Add(absRec);
                return;
            }

            l.Add(absRec);
            EmpDictionaryCsv.Add(id, l);
        }

        private double GetDiffInDays(DateTime current, DateTime last)
        {
            return (current - last).TotalDays;
        }

        private DateTime GetStartDate(List<Absence> absences)
        {
            return absences[0].StartDate;
        }

        private List<KeyValuePair<int, Absence>> ReadCsv(string path)
        {
            var lines = File.ReadAllLines(path);

            List<KeyValuePair<int, Absence>> listAbsence = new List<KeyValuePair<int, Absence>>();

            foreach (var line in lines)
            {
                var split = line.Split(';');
                listAbsence.Add(new KeyValuePair<int, Absence>(int.Parse(split[0]), new Absence
                {
                    StartDate = DateTime.Parse(split[1]),
                    AbsenceType = (AbsenceType) int.Parse(split[2]),
                    Percentage = double.Parse(split[3].Replace(",", "."))

                }));
            }

            return listAbsence;
        }

        #endregion

        #region Xml

        private void UpdateWithXmlFile(FileInfo f)
        {
            try
            {
                using (TextReader reader = new StringReader(File.ReadAllText(f.FullName)))
                {

                    findNode(f.FullName);

                }
            }
            catch (Exception ex)
            {

            }
        }

        private void findNode(string xmlFilePath)
        {
            XmlDocument doc = new XmlDocument();
            DateTime date = DateTime.MaxValue;
            doc.Load(xmlFilePath);
            XmlNode root = doc.DocumentElement;
            List<Employee> Employees = new List<Employee>();

            var serializer = new XmlSerializer(typeof(Employee), new XmlRootAttribute("Employee"));
            foreach (XmlNode n in root.ChildNodes)
            {
                if (n.Name == "Employees")
                {
                    foreach (XmlNode nChildNode in n.ChildNodes)
                    {
                        using (TextReader reader = new StringReader(nChildNode.OuterXml))
                        {
                            var em = (Employee) serializer.Deserialize(reader);
                            //em.Percentage = double.Parse(nChildNode.ChildNodes[3].InnerText);
                            em.EmployeeId = int.Parse(nChildNode.Attributes["EmployeeId"].Value);
                            Employees.Add(em);
                        }


                    }
                }

                if (n.Name == "FileDate")
                {
                    date = DateTime.Parse(n.InnerText);
                }
            }

            EmpDictionaryXml.Add(date, Employees);
        }

        public void ReadXmlFiles(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                DirectoryInfo dirSource = new DirectoryInfo(folderPath);
                var allXMLFiles = dirSource.GetFiles("*.xml", SearchOption.AllDirectories).ToList();

                foreach (var f in allXMLFiles)
                {
                    UpdateWithXmlFile(f);
                }
            }

            //return null;

        }


        #endregion


        public void ScanAndUpdate()
        {
            foreach (var empRec in EmpDictionaryXml)
            {

                FindAndUpdateRecord(empRec);
            }
            //Result is ready from Xml files, start update the csv file ...
            foreach (var emp in result)
            {
                FindAndUpdateRecordsInCsv(emp);
            }

        }

        private void FindAndUpdateRecord(KeyValuePair<DateTime, List<Employee>> empRec)
        {
            if (result.Any())
            {
                //Add new records to result list with elements
                foreach (var rec in empRec.Value)
                {
                    CheckRecordExistAndUpdate(rec);
                }
            }
            else
            {
                result.AddRange(empRec.Value);
            }
        }

        private void CheckRecordExistAndUpdate(Employee rec)
        {
            var foundRecs = result.FirstOrDefault(re => re.EmployeeId == rec.EmployeeId && re.StartDate == rec.StartDate);
            if (foundRecs != null)
            {
                foundRecs.EndDate = rec.EndDate;
                foundRecs.Percentage = rec.Percentage;
                foundRecs.TypeId = rec.TypeId;
            }
            else
            {
                result.Add(rec);
            }

            /*foreach (var item in result)
            {
                if (item.EmployeeId == rec.EmployeeId  && item.StartDate == rec.StartDate)
                {
                    item.EndDate = rec.EndDate;
                    item.Percentage = rec.Percentage;
                    item.TypeId = rec.TypeId;
                }
                else
                {
                    result.Add(rec);
                }
            }*/
        }

        public void WriteNewCsvToFile()
        {
            throw new NotImplementedException();
        }

        private void FindAndUpdateRecordsInCsv(Employee emp)
        {
            if (!EmpDictionaryCsv.ContainsKey(emp.EmployeeId))
            {
                EmpDictionaryCsv.Add(emp.EmployeeId, new List<Absence>());
            }
            var thisEmployeeAbs = EmpDictionaryCsv.FirstOrDefault(i => i.Key == emp.EmployeeId);


            foreach (var d in GetDates(emp.StartDate, emp.EndDate))
            {
                CheckThisDateExistsInCsv(thisEmployeeAbs.Value, d, emp);
            }
        }

        private void CheckThisDateExistsInCsv(List<Absence> thisEmployeeAbs, DateTime d, Employee emp)
        {
            var abs= thisEmployeeAbs.FirstOrDefault(ab=>ab.StartDate==d);
            if (abs == null)
                AddDateToEmployeeAbsences(d, emp);
            else
            {
                abs.Percentage = emp.Percentage;
                abs.AbsenceType = (AbsenceType)emp.TypeId;
            }
        }

        private void AddDateToEmployeeAbsences(DateTime dateTime, Employee emp)
        {
            EmpDictionaryCsv[emp.EmployeeId].Add(new Absence
            {
                StartDate = dateTime,
                AbsenceType = (AbsenceType) emp.TypeId,
                Percentage = emp.Percentage
            });
        }

        private IEnumerable<DateTime> GetDates(DateTime start, DateTime end)
        {
            for (var day = start.Date; day <= end; day = day.AddDays(1))
                yield return day;
        }

        /*private bool AbsenceRange(Absence a, Employee e)
        {

        }*/
    }

    public interface IFileHandler
    {
        public void ReadCsvFile(string file);
        public void ReadXmlFiles(string folderPath);

        void ScanAndUpdate();
        void WriteNewCsvToFile();
    }
}
