namespace CB_Gift.Services.Payments
{
    public class PaymentGatewayFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public PaymentGatewayFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public IPaymentGateway GetGateway(string gatewayName)
        {
            switch (gatewayName.ToUpper())
            {
                case "VNPAY":
                    return _serviceProvider.GetRequiredService<VNPayService>();

                case "PAYOS":
                default:
                    return _serviceProvider.GetRequiredService<PayOSService>();
            }
        }
    }
}
