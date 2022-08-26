using Fitbit.Api.Portable.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SampleWebMVCOAuth2.ViewModels
{
    public class ActivityReportViewModel
    {
        public ActivityReportViewModel()
        {
            RecipientAlerts = new List<string>();
        }

        public List<string> RecipientAlerts { get; set; }
    }
}