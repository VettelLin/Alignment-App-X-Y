using System;
using System.Collections.Generic;
using System.Linq;

namespace General_PCR18.DB
{
    public class PatientDAL : BaseDAL
    {
        public List<Patient> GetList()
        {
            return Execute((conn) =>
            {
                var result = conn.Table<Patient>().ToList();
                return result;
            });
        }


        public Patient FindByPatientId(string patientId)
        {
            return Execute((conn) =>
            {
                try
                {
                    var result = conn.Table<Patient>().Where(t => t.PatientId == patientId).FirstOrDefault();
                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    return null;
                }

            });
        }

        public bool Insert(Patient patient)
        {
            return Execute((conn) =>
            {
                try
                {
                    int rows = conn.Insert(patient);
                    return rows > 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                return false;
            });
        }

    }
}
