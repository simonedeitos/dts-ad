using System;
using System.Collections.Generic;
using System.Linq;

namespace AirDirector.Models
{
    /// <summary>
    /// Schedulazione automatica
    /// </summary>
    public class Schedule
    {
        public enum ScheduleActionType
        {
            PlayClock,      // Riproduce un clock
            PlayAudio,      // Riproduce un file audio singolo
            PlayMiniPLS,    // Riproduce una sequenza MiniPLS
            LogoShow,
            LogoHide
        }

        public int ID { get; set; }
        public string Name { get; set; }
        public ScheduleActionType ActionType { get; set; }

        // Giorni della settimana
        public bool Monday { get; set; }
        public bool Tuesday { get; set; }
        public bool Wednesday { get; set; }
        public bool Thursday { get; set; }
        public bool Friday { get; set; }
        public bool Saturday { get; set; }
        public bool Sunday { get; set; }

        // Orari (lista di stringhe formato HH:mm)
        public List<string> Times { get; set; }

        // Azione specifica
        public string ClockName { get; set; }        // Per PlayClock
        public string AudioFilePath { get; set; }    // Per PlayAudio
        public int? MiniPLSID { get; set; }          // Per PlayMiniPLS

        public Schedule()
        {
            ID = 0;
            Name = string.Empty;
            ActionType = ScheduleActionType.PlayClock;
            Monday = false;
            Tuesday = false;
            Wednesday = false;
            Thursday = false;
            Friday = false;
            Saturday = false;
            Sunday = false;
            Times = new List<string>();
            ClockName = string.Empty;
            AudioFilePath = string.Empty;
            MiniPLSID = null;
        }

        /// <summary>
        /// Verifica se la schedulazione è attiva in un determinato giorno
        /// </summary>
        public bool IsActiveDayOfWeek(DayOfWeek dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case DayOfWeek.Monday: return Monday;
                case DayOfWeek.Tuesday: return Tuesday;
                case DayOfWeek.Wednesday: return Wednesday;
                case DayOfWeek.Thursday: return Thursday;
                case DayOfWeek.Friday: return Friday;
                case DayOfWeek.Saturday: return Saturday;
                case DayOfWeek.Sunday: return Sunday;
                default: return false;
            }
        }

        /// <summary>
        /// Verifica se la schedulazione deve essere eseguita ora
        /// </summary>
        public bool ShouldExecuteNow(DateTime checkTime)
        {
            // Verifica giorno della settimana
            if (!IsActiveDayOfWeek(checkTime.DayOfWeek))
                return false;

            // Verifica orario
            string currentTime = checkTime.ToString("HH:mm");
            return Times.Contains(currentTime);
        }

        /// <summary>
        /// Ottiene la prossima esecuzione
        /// </summary>
        public DateTime? GetNextExecution(DateTime fromTime)
        {
            DateTime? nextExecution = null;

            // Cerca nei prossimi 7 giorni
            for (int dayOffset = 0; dayOffset < 7; dayOffset++)
            {
                DateTime checkDate = fromTime.Date.AddDays(dayOffset);

                if (!IsActiveDayOfWeek(checkDate.DayOfWeek))
                    continue;

                foreach (string time in Times.OrderBy(t => t))
                {
                    if (TimeSpan.TryParse(time, out TimeSpan timeSpan))
                    {
                        DateTime executionTime = checkDate.Add(timeSpan);

                        if (executionTime > fromTime)
                        {
                            if (!nextExecution.HasValue || executionTime < nextExecution.Value)
                            {
                                nextExecution = executionTime;
                            }
                        }
                    }
                }

                if (nextExecution.HasValue)
                    break;
            }

            return nextExecution;
        }

        /// <summary>
        /// Ottiene stringa descrittiva dei giorni attivi
        /// </summary>
        public string GetActiveDaysString()
        {
            List<string> days = new List<string>();
            if (Monday) days.Add("Lun");
            if (Tuesday) days.Add("Mar");
            if (Wednesday) days.Add("Mer");
            if (Thursday) days.Add("Gio");
            if (Friday) days.Add("Ven");
            if (Saturday) days.Add("Sab");
            if (Sunday) days.Add("Dom");

            if (days.Count == 7)
                return "Tutti i giorni";
            else if (days.Count == 0)
                return "Nessun giorno";
            else
                return string.Join(", ", days);
        }

        /// <summary>
        /// Ottiene stringa descrittiva degli orari
        /// </summary>
        public string GetTimesString()
        {
            if (Times.Count == 0)
                return "Nessun orario";
            else if (Times.Count == 1)
                return Times[0];
            else
                return $"{Times.Count} orari";
        }

        /// <summary>
        /// Ottiene descrizione dell'azione
        /// </summary>
        public string GetActionDescription()
        {
            switch (ActionType)
            {
                case ScheduleActionType.PlayClock:
                    return $"Clock: {ClockName}";
                case ScheduleActionType.PlayAudio:
                    return $"Audio: {System.IO.Path.GetFileName(AudioFilePath)}";
                case ScheduleActionType.PlayMiniPLS:
                    return $"Sequenza ID: {MiniPLSID}";
                default:
                    return "Sconosciuto";
            }
        }

        public override string ToString()
        {
            return $"{Name} - {GetActiveDaysString()} - {GetTimesString()}";
        }
    }
}
