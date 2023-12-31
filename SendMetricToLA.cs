﻿using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SendMetricToLA
{
    public class SendMetricToLA
    {
        // Update customerId to your Log Analytics workspace ID
        static string customerId = "";

        // For sharedKey, use either the primary or the secondary Connected Sources client authentication key   
        static string sharedKey = "";

        // LogName is name of the event type that is being submitted to Azure Monitor
        static string LogName = "AutoScalingForTestingKeda";

        // You can use an optional field to specify the timestamp from the data. If the time field is not specified, Azure Monitor assumes the time is the message ingestion time
        static string TimeStampField = "";

        public static void SendMetric(int metricValue)
        {
            var jsonObject = new { AutoScalerMetric_d = metricValue };
            var jsonString = JsonConvert.SerializeObject(jsonObject);
            var json = "[" + jsonString + "]";
            // Create a hash for the API signature
            var datestring = DateTime.UtcNow.ToString("r");
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            string stringToHash = "POST\n" + jsonBytes.Length + "\napplication/json\n" + "x-ms-date:" + datestring + "\n/api/logs";
            string hashedString = BuildSignature(stringToHash, sharedKey);
            string signature = "SharedKey " + customerId + ":" + hashedString;

            PostData(signature, datestring, json);
        }

        // Build the API signature
        public static string BuildSignature(string message, string secret)
        {
            var encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = Convert.FromBase64String(secret);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hash = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hash);
            }
        }

        // Send a request to the POST API endpoint
        public static void PostData(string signature, string date, string json)
        {
            try
            {
                string url = "https://" + customerId + ".ods.opinsights.azure.com/api/logs?api-version=2016-04-01";

                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Log-Type", LogName);
                client.DefaultRequestHeaders.Add("Authorization", signature);
                client.DefaultRequestHeaders.Add("x-ms-date", date);
                client.DefaultRequestHeaders.Add("time-generated-field", TimeStampField);

                HttpContent httpContent = new StringContent(json, Encoding.UTF8);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                Task<HttpResponseMessage> response = client.PostAsync(new Uri(url), httpContent);

                HttpContent responseContent = response.Result.Content;
                string result = responseContent.ReadAsStringAsync().Result;

                Console.WriteLine("status code: " + response.Result.StatusCode.ToString());
                Console.WriteLine("Return Result: " + result);
            }
            catch (Exception excep)
            {
                Console.WriteLine("API Post Exception: " + excep.Message);
            }
        }
    }
}
