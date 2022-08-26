using Fitbit.Api.Portable.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SampleWebMVCOAuth2.ViewModels
{
    public class ActivityLogListViewModel
    {
        public ActivityLogsList LogList { get; set; }
        public int StepGoal { get; set; }
    }
}