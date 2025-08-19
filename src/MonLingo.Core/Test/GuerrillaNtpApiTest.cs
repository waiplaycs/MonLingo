using System;
using System.Net;
using System.Threading.Tasks;
using GuerrillaNtp;

namespace MonLingo.Core.Test
{
    /// <summary>
    /// GuerrillaNtp 2.0.1 API 測試
    /// </summary>
    public class GuerrillaNtpApiTest
    {
        public static void TestBasicApi()
        {
            try
            {
                // 測試基本 API
                var endpoint = new IPEndPoint(IPAddress.Parse("216.239.35.0"), 123); // time.google.com
                
                using (var client = new NtpClient(endpoint))
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    
                    // 嘗試同步方法
                    var response = client.Query();
                    
                    Console.WriteLine($"Response type: {response.GetType()}");
                    Console.WriteLine($"Response properties:");
                    
                    var properties = response.GetType().GetProperties();
                    foreach (var prop in properties)
                    {
                        try
                        {
                            var value = prop.GetValue(response);
                            Console.WriteLine($"  {prop.Name}: {value}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"  {prop.Name}: Error - {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Test failed: {ex.Message}");
            }
        }
    }
}
