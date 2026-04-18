using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Common.Exceptions
{
    public class SubscriptionExpiredException : Exception
    {
        public string ExpiredEndpoint { get; }

        public SubscriptionExpiredException(string endpoint)
            : base($"Push Subscription đã hết hạn hoặc bị người dùng thu hồi quyền. Endpoint: {endpoint}")
        {
            ExpiredEndpoint = endpoint;
        }

        public SubscriptionExpiredException(string endpoint, Exception innerException)
            : base($"Push Subscription đã hết hạn hoặc bị người dùng thu hồi quyền. Endpoint: {endpoint}", innerException)
        {
            ExpiredEndpoint = endpoint;
        }
    }
}
