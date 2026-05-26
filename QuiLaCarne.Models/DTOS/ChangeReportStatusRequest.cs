using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;

    public class ChangeReportStatusRequest
    {
        public string ReportToken { get; set; } = "";

        public DateTimeOffset? ExpiresAt { get; set; }

        public bool Accepted { get; set; }
    }

