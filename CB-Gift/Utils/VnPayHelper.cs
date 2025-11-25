using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Collections.Specialized;
using Microsoft.Extensions.Logging; 
using System.Globalization; 

namespace CB_Gift.Utils
{
    public class VnPayHelper
    {
        private SortedList<string, string> _requestData = new SortedList<string, string>(StringComparer.Ordinal);
        private readonly ILogger<VnPayHelper> _logger;

        public VnPayHelper(ILogger<VnPayHelper> logger)
        {
            _logger = logger;
        }

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        //  Hàm này để reset helper khi dùng DI
        public void ClearRequestData()
        {
            _requestData.Clear();
        }

        public string CreateRequestUrl(string baseUrl, string hashSecret)
        {
            var data = new StringBuilder();

            // Logic này khớp với code mẫu (Encode cả key và value)
            foreach (var (key, value) in _requestData)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    //  mã hóa cả key và value
                    data.Append(WebUtility.UrlEncode(key) + "=" + WebUtility.UrlEncode(value) + "&");
                }
            }
            string queryString = data.ToString().TrimEnd('&'); // Dùng TrimEnd an toàn hơn

            // === DÒNG DEBUG QUAN TRỌNG ===
            _logger.LogInformation("--- VNPAY DEBUG (CreateRequest): Dữ liệu đang được HASH ---");
            _logger.LogInformation(queryString);
            // ===============================

            //  dùng HmacSHA512
            string hmac = HmacSHA512(hashSecret, queryString);

            return baseUrl + "?" + queryString + "&vnp_SecureHash=" + hmac;
        }

        public bool ValidateSignatureFromQueryString(string inputHash, string hashSecret, string queryString)
        {
            var vnpayData = new SortedList<string, string>(StringComparer.Ordinal);
            var queryParams = System.Web.HttpUtility.ParseQueryString(queryString.StartsWith("?") ? queryString.Substring(1) : queryString);

            foreach (var key in queryParams.AllKeys)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpayData.Add(key, queryParams[key]);
                }
            }

            vnpayData.Remove("vnp_SecureHash");
            if (vnpayData.ContainsKey("vnp_SecureHashType"))
            {
                vnpayData.Remove("vnp_SecureHashType");
            }

            var data = new StringBuilder();
           
            foreach (var (key, value) in vnpayData)
            {
                //  mã hóa cả key và value
                data.Append(WebUtility.UrlEncode(key) + "=" + WebUtility.UrlEncode(value) + "&");
            }
            string dataToHash = data.ToString().TrimEnd('&'); // Dùng TrimEnd an toàn hơn

            // === DÒNG DEBUG QUAN TRỌNG ===
            _logger.LogInformation("--- VNPAY DEBUG (ValidateSignature): Dữ liệu đang được VALIDATE ---");
            _logger.LogInformation(dataToHash);
            // ===============================

            // dùng HmacSHA512
            string myHash = HmacSHA512(hashSecret, dataToHash);
            return myHash.Equals(inputHash, StringComparison.OrdinalIgnoreCase);
        }

        private string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }
        private string HmacSHA256(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }
    }
}