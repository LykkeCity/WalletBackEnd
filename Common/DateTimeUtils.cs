using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Common
{


    public interface IWeeklyPeriod
    {
        DayOfWeek DayOfWeekFrom { get; }
        string TimeFrom { get; }
        DayOfWeek DayOfWeekTo { get; }
        string TimeTo { get; }
    }

    public class WeeklyPeriod : IWeeklyPeriod
    {
        public DayOfWeek DayOfWeekFrom { get; set; }
        public string TimeFrom { get; set; }
        public DayOfWeek DayOfWeekTo { get; set; }
        public string TimeTo { get; set; }
    }


    public class WeeklyTime
    {
        public DayOfWeek DayOfWeek { get; set; }
        public string Time { get; set; }

        public static WeeklyTime Create(DayOfWeek dayOfWeek, string time)
        {
            return new WeeklyTime
            {
                DayOfWeek = dayOfWeek,
                Time = time
            };
        }
    }

  
    public class WeekTimePair
    {
        private static readonly Dictionary<DayOfWeek, DateTime> InitDict = new Dictionary<DayOfWeek, DateTime>(); 

        static WeekTimePair()
        {
            InitDict.Add(DayOfWeek.Monday, DateTime.Parse("2013-01-07"));
            InitDict.Add(DayOfWeek.Tuesday, DateTime.Parse("2013-01-08"));
            InitDict.Add(DayOfWeek.Wednesday, DateTime.Parse("2013-01-09"));
            InitDict.Add(DayOfWeek.Thursday, DateTime.Parse("2013-01-10"));
            InitDict.Add(DayOfWeek.Friday, DateTime.Parse("2013-01-11"));
            InitDict.Add(DayOfWeek.Saturday, DateTime.Parse("2013-01-12"));
            InitDict.Add(DayOfWeek.Sunday, DateTime.Parse("2013-01-13"));
        }

        private DateTime _dateTime;

        public static void ParseTime(string time, out int hour, out int min, out int sec)
        {
            hour = 0;
            min = 0;
            sec = 0;

            var strs = time.Split(':');

            switch (strs.Length)
            {
                case 0:
                    throw new Exception("Invalid time format: " + time);

                case 1:
                    sec = int.Parse(strs[0]);
                    break;

                case 2:
                    min = int.Parse(strs[0]);
                    sec = int.Parse(strs[1]);
                    break;

                default:
                    hour = int.Parse(strs[0]);
                    min = int.Parse(strs[1]);
                    sec = int.Parse(strs[2]);
                    break;
            }

        }

        private void Init(DayOfWeek dayOfWeek, int hour, int min, int sec)
        {
            var date = InitDict[dayOfWeek];
            _dateTime = new DateTime(date.Year, date.Month, date.Day, hour, min, sec);
        }

        public WeekTimePair(DayOfWeek dayOfWeek, string time)
        {
            int hour, min, sec;
            ParseTime(time, out hour, out min, out sec);
            Init(dayOfWeek, hour, min, sec);

        }

        public WeekTimePair(DayOfWeek dayOfWeek, int hour, int min, int sec)
        {
            Init(dayOfWeek, hour, min, sec);
        }

        internal WeekTimePair(DateTime dateTime)
        {
            _dateTime = dateTime;
        }

        public DayOfWeek DayOfWeek { get { return _dateTime.DayOfWeek; } }

        public int Hour { get { return _dateTime.Hour; } }
        public int Minut { get { return _dateTime.Minute; } }
        public int Second { get { return _dateTime.Second; } }

        public WeekTimePair AddMinutes(int minutes)
        {
            return new WeekTimePair(_dateTime.AddMinutes(minutes)); 
        }

        public string TimeAsString()
        {
            return Hour.ToString("00") + ":" + Minut.ToString("00") + ":" + Second.ToString("00");
        }

    }

    public static class DateTimeUtils
    {
        private static readonly DateTime Jan1St1970 = new DateTime(1970, 1, 1);
        public static long ToJavaScriptIntDate(this DateTime dateTime)
        {
            return Convert.ToInt64((dateTime - Jan1St1970).TotalMilliseconds);
        }

        public static IEnumerable<WeekTimePair> GeneratePairs(DayOfWeek dayOfWeekFrom, string timeFrom, int minutesDelta,
            int count)
        {
            WeekTimePair weekTimePair = null;

            for (var i = 0; i < count; i++)
            {
                weekTimePair = weekTimePair == null
                    ? new WeekTimePair(dayOfWeekFrom, timeFrom)
                    : weekTimePair.AddMinutes(minutesDelta);
                yield return weekTimePair;
            }
        }
        
        public const int SecondsInDay = 86400;
        public const int MinutesInDay = 1440;
        
        /// <summary>
        /// Количество секунд с воскресения
        /// </summary>
        /// <param name="dayOfWeek">день недели</param>
        /// <param name="time">время</param>
        /// <returns>количество секунд с воскресенья. Sunday 00:00:00 = 0</returns>
        public static int DateTimeToInt(DayOfWeek dayOfWeek, string time)
        {
            int hour, min, sec;
            WeekTimePair.ParseTime(time, out hour, out min, out sec);

            return ((int) dayOfWeek)*SecondsInDay + hour*3600 + min*60 + sec;

        }


        public static int DateTimeToInt(DateTime dateTime)
        {
           return DateTimeToInt(dateTime.DayOfWeek, GetTime(dateTime));
        }


        public static IEnumerable<DateTime> EnumerateDatesMin(DateTime from, int addMin, int count)
        {
            for (var i = 0; i < count; i++)
            {
                yield return from;
                from = from.AddMinutes(addMin);
            }
        }

        /// <summary>
        /// От 0 - 100
        /// </summary>
        /// <returns></returns>
        public static int ProgressOfTime(DateTime from, DateTime to, DateTime now)
        {
            var totalSecs = (to - from).TotalSeconds;
            var pastSecs = (now - from).TotalSeconds;

            if (totalSecs <= 0)
                return 0;

            return (int)Math.Round(pastSecs/totalSecs*100);
        }

        public static double ProgressOfTimeFloat(DateTime from, DateTime to, DateTime now)
        {
            var totalSecs = (to - from).TotalSeconds;
            var pastSecs = (now - from).TotalSeconds;

            if (totalSecs <= 0)
                return 0;

            return Math.Round(pastSecs / totalSecs * 100, 2);
        }

        /// <summary>
        /// Проверяем место нахождение недели в заданном диапазоне. Левый порог включен, правый порог не включен
        /// </summary>
        /// <param name="period"></param>
        /// <param name="myDayOfWeek"></param>
        /// <param name="myTime"></param>
        /// <returns></returns>
        public static bool IsBetween(this IWeeklyPeriod period, DayOfWeek myDayOfWeek, string myTime)
        {

            var from = DateTimeToInt(period.DayOfWeekFrom, period.TimeFrom);
            var to = DateTimeToInt(period.DayOfWeekTo, period.TimeTo);
            var my = DateTimeToInt(myDayOfWeek, myTime);

            if (from == my && to == my)
                return true;

            if (from < to)
                return from <= my && my < to;

            return my>=from || my <= to;
        }

        public static T GetBetweenElement<T>(this IEnumerable<T> periods, DayOfWeek myDayOfWeek, string myTime) where T : class,IWeeklyPeriod
        {
            return periods.FirstOrDefault(itm => itm.IsBetween(myDayOfWeek, myTime));
        }
        public static T GetBetweenElement<T>(this IEnumerable<T> periods, DateTime dateTime) where T : class,IWeeklyPeriod
        {
            return periods.FirstOrDefault(itm => itm.IsBetween(dateTime.DayOfWeek, dateTime.GetTime()));
        }


        public static bool IsBetween(this IWeeklyPeriod period, DateTime nowDateTime)
        {
            var dow = nowDateTime.DayOfWeek;
            var dowTime = GetTime(nowDateTime);
            return IsBetween(period, dow, dowTime);
        }

        public static IEnumerable<DayOfWeek> GetAllDaysOfWeek()
        {
            yield return DayOfWeek.Sunday;
            yield return DayOfWeek.Monday;
            yield return DayOfWeek.Tuesday;
            yield return DayOfWeek.Wednesday;
            yield return DayOfWeek.Thursday;
            yield return DayOfWeek.Friday;
            yield return DayOfWeek.Saturday;
        }

        public static string FormatChromeDateTime(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd") + "T" + dateTime.ToString("hh:mm");
        }

        public static string StandartDateTimeMask
        {
            get
            {
                return "dd.MM.yy HH:mm:ss";
            }
        }

        public static string StandartDate
        {
            get
            {
                return "dd/MM/yyyy";
            }
        }

        public static DateTime TruncMiliseconds(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second);
        }

        public static DateTime RoundSeconds(DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0);
        }

        public static DateTime RoundToMinutes(DateTime dateTime, int minInterval)
        {
            var min = dateTime.Minute / minInterval * minInterval;
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, min, 0);
        }

        // YYYY-MM-DD 12:00:00
        public static bool IsIsoDateTimeString(string value)
        {
            value = value.Trim();

            if (value.Length < 19)
                return false;


            if (!value[0].IsDigit())
                return false;

            if (!value[1].IsDigit())
                return false;

            if (!value[2].IsDigit())
                return false;

            if (!value[3].IsDigit())
                return false;

            if (value[4] != '-')
                return false;

            if (!value[5].IsDigit())
                return false;

            if (!value[6].IsDigit())
                return false;

            if (value[7] != '-')
                return false;

            if (!value[8].IsDigit())
                return false;

            if (!value[9].IsDigit())
                return false;

            if (value[10] != ' ')
                return false;

            if (!value[11].IsDigit())
                return false;

            if (!value[12].IsDigit())
                return false;

            if (value[13] != ':')
                return false;

            if (!StringUtils.IsDigit(value[14]))
                return false;

            if (!StringUtils.IsDigit(value[15]))
                return false;

            if (value[16] != ':')
                return false;

            if (!StringUtils.IsDigit(value[17]))
                return false;

            if (!StringUtils.IsDigit(value[18]))
                return false;

            return true;

        }

        public static bool IsTimeStringValid(string time,bool hourCanBeMoreThan23 = false)
        {
            if (string.IsNullOrEmpty(time))
                return false;

            try
            {
                var strs = time.Split(':');

                if (strs.Length == 0)
                    return false;

                if (strs.Length > 3)
                    return false;

                int h, m, s;

                WeekTimePair.ParseTime(time, out h, out m, out s);

                if (h < 0)
                    return false;

                if (!hourCanBeMoreThan23)
                    if (h > 23)
                        return false;

                if (m < 0 || m > 59)
                    return false;

                if (s < 0 || s > 59)
                    return false;

                return true;
            }
            catch (Exception)
            {

                return false;
            }

        }

        public static int CalcMinutes(string time)
        {
            int h, m, s;

            WeekTimePair.ParseTime(time, out h, out m, out s);

            return h*60 + m;
        }

        public static int CalcSeconds(string time)
        {
            int h, m, s;

            WeekTimePair.ParseTime(time, out h, out m, out s);

            return h * 3600 + m*60 + s;
        }

        public static string MinutesToString(int minutes)
        {
            var h = minutes/60;
            minutes -= h*60;


            return h.ToString("00") + ":" + minutes.ToString("00") + ":00";
        }
        public static string SecondsToString(int seconds)
        {
            var h = seconds / 3600;
            seconds -= h * 3600;

            var m = seconds/60;
            seconds -= m * 60;

            return h.ToString("00") + ":" + m.ToString("00") + ":"+seconds.ToString("00");
        }

        public static IEnumerable<int> GenerateYearsToNow(int fromYear)
        {
            var year = DateTime.UtcNow.Year;
            var result = new List<int>();
            for (var i = fromYear; i <= year; i++)
                result.Add(i);

            return result;
        }

        public const string RegExTime = @"([01]?[0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]";

        public static string GetTime(this DateTime dateTime)
        {
            return dateTime.Hour.ToString("00") + ":" + dateTime.Minute.ToString("00") + ":" + dateTime.Second.ToString("00");
        }

        public static DateTime ParseIsoDateTime(this string value)
        {
            value = value.Replace('_', ' ');
            DateTime result;
            var isOk = DateTime.TryParseExact(value, Utils.IsoDateTimeMask,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out result);

            if (!isOk)
                throw new Exception("Invalid datetime ISO format "+value);

            return result;
        }

        public static DateTime ParseIsoDate(this string value)
        {
            DateTime result;
            var isOk = DateTime.TryParseExact(value, Utils.IsoDateMask,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out result);

            if (!isOk)
                throw new Exception("Invalid datetime ISO format " + value);

            return result;
        }


        /// <summary>
        ///  Уменьшаем точность до минуты, отбрасывая секунды
        /// </summary>
        /// <param name="dateTime">Исходное дата-время</param>
        /// <returns>Округленная дата-время</returns>
        public static DateTime RoundToMinute(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year,dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0);
        }


        /// <summary>
        ///  Уменьшаем точность до 5 минут
        /// </summary>
        /// <param name="dateTime">Исходное дата-время</param>
        /// <param name="min">5 - округляем до 5 минут</param>
        /// <returns>Округленная дата-время</returns>
        public static DateTime RoundToMinute(this DateTime dateTime, int min)
        {
            var part = dateTime.Minute / min;

            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, part * min, 0);
        }


        /// <summary>
        ///  Уменьшаем точность до часа - отбрасывая минуты
        /// </summary>
        /// <param name="dateTime">Исходное дата-время</param>
        /// <returns>Округленная дата-время</returns>
        public static DateTime RoundToHour(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0);
        }

        /// <summary>
        ///  Уменьшаем точность до часа - отбрасывая минуты
        /// </summary>
        /// <param name="dateTime">Исходное дата-время</param>
        /// <returns>Округленная дата-время</returns>
        public static DateTime RoundToMonth(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, 1, 0, 0, 0);
        }

        /// <summary>
        ///  Уменьшаем точность до года - отбрасывая все остальное
        /// </summary>
        /// <param name="dateTime">Исходное дата-время</param>
        /// <returns>Округленная дата-время</returns>
        public static DateTime RoundToYear(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, 1, 1, 0, 0, 0);
        }


        public static string ToIsoDate(this DateTime dateTime)
        {
            return dateTime.Year + "-" + dateTime.Month.ToString("00", CultureInfo.InvariantCulture) + "-" +
                   dateTime.Day.ToString("00", CultureInfo.InvariantCulture);
        }

        public static string ToTimeString(this DateTime dateTime, char separator = ':')
        {
            return dateTime.Hour.ToString("00") + separator + dateTime.Minute.ToString("00") + separator +
                   dateTime.Second.ToString("00");
        }

        public static string ToIsoDateTime(this DateTime dateTime)
        {
            return dateTime.ToString(Utils.IsoDateTimeMask);
        }


        public static string ToYyyyMmDd(this DateTime dateTime)
        {
            return dateTime.ToString("yyyyMMdd");
        }

        /// <summary>
        /// Сгенерировать даты (без времени)
        /// </summary>
        /// <param name="dateFrom">дата с которой начать</param>
        /// <param name="dateTo">дата которой закончить (включительно)</param>
        /// <returns></returns>
        public static IEnumerable<DateTime> GenerateDates(DateTime dateFrom, DateTime dateTo)
        {
            dateFrom = dateFrom.Date;
            dateTo = dateTo.Date;

            while (dateFrom <= dateTo)
            {
                yield return dateFrom;
                dateFrom = dateFrom.AddDays(1);
            }
        }

        /// <summary>
        /// Сгенерировать даты за определенный месяц
        /// </summary>
        public static IEnumerable<DateTime> GenerateMonthDates(int year, int month)
        {
            var dateFrom = new DateTime(year, month, 1);
            var dateTo = dateFrom.AddDays(DateTime.DaysInMonth(year, month)-1);

            return GenerateDates(dateFrom, dateTo);
        }

        public static DateTime SetTime(this DateTime src, int hours, int mins, int seconds)
        {
            return new DateTime(src.Year, src.Month, src.Day, hours, mins, seconds);
        }

        public static string YearLastTwoDigits(this int year)
        {
            var result = year.ToString(CultureInfo.InvariantCulture);

            if (result.Length == 4)
                return result.Substring(2,2);

            return result;
        }


        public static string ToHtmlComponentDate(this DateTime dateTime)
        {
            return dateTime.Day.ToString("00") + "/" + dateTime.Month.ToString("00") + "/" + dateTime.Year;
        }


        private static DateTime _baseDateTime = new DateTime(1970,1,1);

        public static DateTime FromUnixDateTime(this uint unixDateTime)
        {
            return _baseDateTime.AddSeconds(unixDateTime);
        }

        public static string ToDateString(this DateTime? value)
        {
            return value?.ToShortDateString();
        }

    }



}
