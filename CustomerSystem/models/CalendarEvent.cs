using Plugin.Maui.Calendar.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Xml.Linq;

namespace CustomerSystem.Models
{
    public class CalendarEvent
    {
        public DateTime Date { get; set; }
        public string Name { get; set; } = "有交易資料";
    }
}