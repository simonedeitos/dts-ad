using System;
using System.Collections.Generic;
using System.Linq;

namespace AirDirector.Models
{
    /// <summary>
    /// Calendario di validità per brani e clips
    /// </summary>
    public class ValidationCalendar
    {
        // Mesi validi (1-12)
        public List<int> ValidMonths { get; set; }

        // Giorni settimana validi (Monday, Tuesday, etc.)
        public List<DayOfWeek> ValidDays { get; set; }

        // Ore valide (0-23)
        public List<int> ValidHours { get; set; }

        // Date di validità assolute
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }

        public ValidationCalendar()
        {
            // Default: sempre valido
            ValidMonths = Enumerable.Range(1, 12).ToList();
            ValidDays = new List<DayOfWeek>
            {
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday,
                DayOfWeek.Sunday
            };
            ValidHours = Enumerable.Range(0, 24).ToList();
            ValidFrom = null;
            ValidTo = null;
        }

        /// <summary>
        /// Verifica se il brano è valido in un determinato momento
        /// </summary>
        public bool IsValid(DateTime checkTime)
        {
            // Verifica data inizio
            if (ValidFrom.HasValue && checkTime < ValidFrom.Value)
                return false;

            // Verifica data fine
            if (ValidTo.HasValue && checkTime > ValidTo.Value)
                return false;

            // Verifica mese
            if (!ValidMonths.Contains(checkTime.Month))
                return false;

            // Verifica giorno settimana
            if (!ValidDays.Contains(checkTime.DayOfWeek))
                return false;

            // Verifica ora
            if (!ValidHours.Contains(checkTime.Hour))
                return false;

            return true;
        }

        /// <summary>
        /// Serializza i mesi in stringa CSV
        /// </summary>
        public string GetMonthsAsString()
        {
            return string.Join(";", ValidMonths);
        }

        /// <summary>
        /// Deserializza i mesi da stringa CSV
        /// </summary>
        public void SetMonthsFromString(string monthsString)
        {
            ValidMonths.Clear();
            if (string.IsNullOrEmpty(monthsString))
                return;

            foreach (string month in monthsString.Split(';'))
            {
                if (int.TryParse(month, out int m))
                    ValidMonths.Add(m);
            }
        }

        /// <summary>
        /// Serializza i giorni in stringa CSV
        /// </summary>
        public string GetDaysAsString()
        {
            return string.Join(";", ValidDays.Select(d => d.ToString()));
        }

        /// <summary>
        /// Deserializza i giorni da stringa CSV
        /// </summary>
        public void SetDaysFromString(string daysString)
        {
            ValidDays.Clear();
            if (string.IsNullOrEmpty(daysString))
                return;

            foreach (string day in daysString.Split(';'))
            {
                if (Enum.TryParse<DayOfWeek>(day, out DayOfWeek d))
                    ValidDays.Add(d);
            }
        }

        /// <summary>
        /// Serializza le ore in stringa CSV
        /// </summary>
        public string GetHoursAsString()
        {
            return string.Join(";", ValidHours);
        }

        /// <summary>
        /// Deserializza le ore da stringa CSV
        /// </summary>
        public void SetHoursFromString(string hoursString)
        {
            ValidHours.Clear();
            if (string.IsNullOrEmpty(hoursString))
                return;

            foreach (string hour in hoursString.Split(';'))
            {
                if (int.TryParse(hour, out int h))
                    ValidHours.Add(h);
            }
        }
    }
}