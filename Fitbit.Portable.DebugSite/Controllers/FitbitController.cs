﻿using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Fitbit.Api;
using System.Configuration;
using Fitbit.Models;
using Fitbit.Api.Portable;
using System.Threading.Tasks;
using Fitbit.Api.Portable.OAuth2;
using SampleWebMVCOAuth2.Models;
using SampleWebMVCOAuth2.ViewModels;
using System.Linq;
using System.Security.Cryptography;

namespace SampleWebMVC.Controllers
{
    public class FitbitController : Controller
    {
        //
        // GET: /Fitbit/

        public ActionResult Index()
        {
            return RedirectToAction("Index", "Home");
        }

        private string GenerateRandomNumberLengthOf(int length)
        {
            string num = "";
            var rando = new Random();            
            for(int i=0; i< length; i++)
            {
                num = num + rando.Next(0, 10).ToString();
            }
            return num;
        }

        //
        // GET: /FitbitAuth/
        // Setup - prepare the user redirect to Fitbit.com to prompt them to authorize this app.
        public ActionResult Authorize()
        {
            var appCredentials = new FitbitAppCredentials() {
                    ClientId = ConfigurationManager.AppSettings["FitbitClientId"],
                    ClientSecret = ConfigurationManager.AppSettings["FitbitClientSecret"]
            };
            //make sure you've set these up in Web.Config under <appSettings>:

            Session["AppCredentials"] = appCredentials;            
            Session["CodeVerifier"] = GenerateRandomNumberLengthOf(50);

            //Provide the App Credentials. You get those by registering your app at dev.fitbit.com
            //Configure Fitbit authenticaiton request to perform a callback to this constructor's Callback method
            var authenticator = new OAuth2Helper(appCredentials, Request.Url.GetLeftPart(UriPartial.Authority) + "/Fitbit/Callback2", Session["CodeVerifier"].ToString());
            string[] scopes = new string[] {"profile", "weight", "activity", "sleep"};
            
            string authUrl = authenticator.GenerateAuthUrl(scopes, null);

            return Redirect(authUrl);
        }

        public async Task<ActionResult> Callback2()
        {
            FitbitAppCredentials appCredentials = (FitbitAppCredentials)Session["AppCredentials"];

            var authenticator = new OAuth2Helper(appCredentials, Request.Url.GetLeftPart(UriPartial.Authority) + "/Fitbit/Callback2", Session["CodeVerifier"].ToString());

            string code = Request.Params["code"];

            OAuth2AccessToken accessToken = await authenticator.ExchangeAuthCodeForAccessTokenAsync(code);            

            //Store credentials in FitbitClient. The client in its default implementation manages the Refresh process
            var fitbitClient = GetFitbitClient(accessToken);
            fitbitClient.AccessToken = accessToken;

            ViewBag.AccessToken = accessToken;

            return View("Callback");

        }

        //Final step. Take this authorization information and use it in the app
        //public async Task<ActionResult> Callback()
        //{
        //    FitbitAppCredentials appCredentials = (FitbitAppCredentials)Session["AppCredentials"];

        //    var authenticator = new OAuth2Helper(appCredentials, Request.Url.GetLeftPart(UriPartial.Authority) + "/Fitbit/Callback");

        //    string code = Request.Params["code"];

        //    OAuth2AccessToken accessToken = await authenticator.ExchangeAuthCodeForAccessTokenAsync(code);

        //    //Store credentials in FitbitClient. The client in its default implementation manages the Refresh process
        //    var fitbitClient = GetFitbitClient(accessToken);
        //    fitbitClient.AccessToken = accessToken;

        //    ViewBag.AccessToken = accessToken;

        //    return View();

        //}

        /// <summary>
        /// In this example we show how to explicitly request a token refresh. However, FitbitClient V2 on its default implementation provide an OOB automatic token refresh.
        /// </summary>
        /// <returns>A refreshed token</returns>
        public async Task<ActionResult> RefreshToken()
        {
            var fitbitClient = GetFitbitClient();

            ViewBag.AccessToken = await fitbitClient.RefreshOAuth2TokenAsync();

            return View("Callback");
        }

        public async Task<ActionResult> TestToken()
        {
            var fitbitClient = GetFitbitClient();

            ViewBag.AccessToken = fitbitClient.AccessToken;

            ViewBag.UserProfile = await fitbitClient.GetUserProfileAsync();

            return View("TestToken");
        }       

        [HttpGet()]
        public async Task<ActionResult> ActivityGoals()
        {
            FitbitClient client = GetFitbitClient();

            var response = await client.GetGoalsAsync(GoalPeriod.Daily);

            return View(response);

        }
        
        
        [HttpGet()]
        public async Task<ActionResult> Activity()
        {
            FitbitClient client = GetFitbitClient();
            var response = await client.GetDayActivityAsync(DateTime.Now);
            return View(response);
        }

        [HttpGet()]
        public async Task<ActionResult> ActivityLogList()
        {
            FitbitClient client = GetFitbitClient();
            var response = await client.GetActivityLogsListAsync(null, DateTime.Now.AddDays(-30),5);
            response.Activities = response.Activities
                .OrderBy(a => a.DateOfActivity)
                .ToList();

            var mdl = new ActivityLogListViewModel()
            {
                LogList = response,
                StepGoal = 3000    //TODO: Get from WebAPI
            };
            return View(mdl);
        }

        [HttpGet()]
        public async Task<ActionResult> ActivityReport()
        {
            FitbitClient client = GetFitbitClient();
            var mdl = new ActivityReportViewModel();
            List<String> Recipients = new List<string>()
            {
                "Brian Gerdon"
            };
            foreach (var recipient in Recipients)
            {
                var response = await client.GetActivityLogsListAsync(null, DateTime.Now.AddDays(-30), 30);
                response.Activities = response.Activities
                    .OrderByDescending(a => a.DateOfActivity)
                    .ToList();
            
                
                bool alert = false;

                List<Fitbit.Api.Portable.Models.Activities> activities = response.Activities;
                activities.OrderByDescending(x => x.DateOfActivity);
                for(int i = 0; i < activities.Count; i++)
                {
                    if(i == activities.Count - 1)
                    {
                        break;
                    }
                    if(activities[i].Steps < (activities[i+1].Steps * .9))
                    {
                        alert = true;
                    }
                    if(alert)
                    {
                        break;
                    }
                }
                
                if(alert == true)
                {
                    mdl.RecipientAlerts.Add(recipient);
                }
            }

            return View(mdl);
        }

        //[HttpPost()]
        //public async Task<ActionResult> ActivityGoals(ActivityGoals goals)
        //{
        //    FitbitClient client = GetFitbitClient();

        //    var response = await client.SetGoalsAsync(goals.CaloriesOut, (decimal)goals.Distance, (goals.Floors.HasValue ? goals.Floors.Value : default(int)), 
        //        goals.Steps, (goals.ActiveMinutes.HasValue ? goals.ActiveMinutes.Value : default(int)), period: GoalPeriod.Daily);

        //    return View(response);

        //}

        [HttpGet()]
        public async Task<ActionResult> WeightGoal()
        {
            FitbitClient client = GetFitbitClient();

            var response = await client.GetWeightGoalsAsync();

            return View(response);
        }

        //[HttpPost()]
        //public async Task<ActionResult> WeightGoal(WeightGoal weightGoal)
        //{
        //    FitbitClient client = GetFitbitClient();

        //    var response = await client.SetWeightGoalAsync(weightGoal.StartDate, weightGoal.StartWeight, weightGoal.Weight);

        //    return View(response);
        //}

        //[HttpGet()]
        //public async Task<ActionResult> SleepGoal()
        //{
        //    FitbitClient client = GetFitbitClient();

        //    var response = await client.GetSleepGoalAsync();

        //    return View(response);
        //}

        /// <summary>
        /// HttpClient and hence FitbitClient are designed to be long-lived for the duration of the session. This method ensures only one client is created for the duration of the session.
        /// More info at: http://stackoverflow.com/questions/22560971/what-is-the-overhead-of-creating-a-new-httpclient-per-call-in-a-webapi-client
        /// </summary>
        /// <returns></returns>
        private FitbitClient GetFitbitClient(OAuth2AccessToken accessToken = null)
        {
            if (Session["FitbitClient"] == null)
            {
                if (accessToken != null)
                {
                    var appCredentials = (FitbitAppCredentials)Session["AppCredentials"];
                    FitbitClient client = new FitbitClient(appCredentials, accessToken);
                    Session["FitbitClient"] = client;
                    return client;
                }
                else
                {
                    throw new Exception("First time requesting a FitbitClient from the session you must pass the AccessToken.");
                }

            }
            else
            {
                return (FitbitClient)Session["FitbitClient"];
            }
        }
    }

    /*
       public string TestTimeSeries()
       {
           FitbitClient client = GetFitbitClient();

           var results = client.GetTimeSeries(TimeSeriesResourceType.DistanceTracker, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

           string sOutput = "";
           foreach (var result in results.DataList)
           {
               sOutput += result.DateTime.ToString() + " - " + result.Value.ToString();
           }

           return sOutput;

       }

       public ActionResult LastWeekDistance()
       {
           FitbitClient client = GetFitbitClient();

           TimeSeriesDataList results = client.GetTimeSeries(TimeSeriesResourceType.Distance, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

           return View(results);
       }
       */

    //public async Task<ActionResult> LastWeekSteps()
    //{

    //    FitbitClient client = GetFitbitClient();

    //    var response = await client.GetTimeSeriesIntAsync(TimeSeriesResourceType.Steps, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

    //    return View(response);

    //}
    /*
    //example using the direct API call getting all the individual logs
    public ActionResult MonthFat(string id)
    {
        DateTime dateStart = Convert.ToDateTime(id);

        FitbitClient client = GetFitbitClient();

        Fat fat = client.GetFat(dateStart, DateRangePeriod.OneMonth);

        if (fat == null || fat.FatLogs == null) //succeeded but no records
        {
            fat = new Fat();
            fat.FatLogs = new List<FatLog>();
        }
        return View(fat);

    }

    //example using the time series, one per day
    public ActionResult LastYearFat()
    {
        FitbitClient client = GetFitbitClient();

        TimeSeriesDataList fatSeries = client.GetTimeSeries(TimeSeriesResourceType.Fat, DateTime.UtcNow, DateRangePeriod.OneYear);

        return View(fatSeries);

    }

    //example using the direct API call getting all the individual logs
    public ActionResult MonthWeight(string id)
    {
        DateTime dateStart = Convert.ToDateTime(id);

        FitbitClient client = GetFitbitClient();

        Weight weight = client.GetWeight(dateStart, DateRangePeriod.OneMonth);

        if (weight == null || weight.Weights == null) //succeeded but no records
        {
            weight = new Weight();
            weight.Weights = new List<WeightLog>();
        }
        return View(weight);

    }

    //example using the time series, one per day
    public ActionResult LastYearWeight()
    {
        FitbitClient client = GetFitbitClient();

        TimeSeriesDataList weightSeries = client.GetTimeSeries(TimeSeriesResourceType.Weight, DateTime.UtcNow, DateRangePeriod.OneYear);

        return View(weightSeries);

    }

    /// <summary>
    /// This requires the Fitbit staff approval of your app before it can be called
    /// </summary>
    /// <returns></returns>
    public string TestIntraDay()
    {
        FitbitClient client = new FitbitClient(ConfigurationManager.AppSettings["FitbitConsumerKey"],
            ConfigurationManager.AppSettings["FitbitConsumerSecret"],
            Session["FitbitAuthToken"].ToString(),
            Session["FitbitAuthTokenSecret"].ToString());

        IntradayData data = client.GetIntraDayTimeSeries(IntradayResourceType.Steps, new DateTime(2012, 5, 28, 11, 0, 0), new TimeSpan(1, 0, 0));

        string result = "";

        foreach (IntradayDataValues intraData in data.DataSet)
        {
            result += intraData.Time.ToShortTimeString() + " - " + intraData.Value + Environment.NewLine;
        }

        return result;

    }

     */

}
