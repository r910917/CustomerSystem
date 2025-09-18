using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomerSystem.Pages;
using Plugin.Maui.Calendar.Models;

namespace CustomerSystem.Models
{
    public class Transactiondata
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public decimal Amount { get; set; }
        public decimal InitialBalance { get; set; }
        public DateTime Date { get; set; }
        public string Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public double Balance { get; set; }
    }

}

